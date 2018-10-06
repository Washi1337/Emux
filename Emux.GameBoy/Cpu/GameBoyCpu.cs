using System;
using System.Collections.Generic;
using System.Threading;
using Emux.GameBoy.Graphics;

namespace Emux.GameBoy.Cpu
{
    /// <summary>
    /// Represents a central processing unit of a GameBoy device.
    /// </summary>
    public class GameBoyCpu : IGameBoyComponent
    {
        public const int VerticalBlankIsr = 0x0040;
        public const int LcdStatusIsr = 0x0048;
        public const int TimerOverflowIsr = 0x0050;
        public const int SerialLinkIsr = 0x0058;
        public const int JoypadPressIsr = 0x0060;
        public const double OfficialClockFrequency = 4194304;

        /// <summary>
        /// Occurs when the processor has resumed execution.
        /// </summary>
        public event EventHandler Resumed;

        /// <summary>
        /// Occurs when the processor is paused by breaking the execution explicitly, or when the control flow hit a breakpoint.
        /// </summary>
        public event EventHandler Paused;

        /// <summary>
        /// Occurs when the process has completely shut down.
        /// </summary>
        public event EventHandler Terminated;

        public event StepEventHandler PerformedStep;
        
        private readonly Z80Disassembler _disassembler;
        private readonly GameBoy _device;
        private readonly ManualResetEvent _continueSignal = new ManualResetEvent(false);
        private readonly ManualResetEvent _terminateSignal = new ManualResetEvent(false);
        
        private ulong _ticks;
        private bool _break = true;
        private bool _halt = false;
        private readonly ManualResetEvent _frameStartSignal = new ManualResetEvent(false);
        private readonly ManualResetEvent _breakSignal = new ManualResetEvent(false);
        
        private TimeSpan _frameStartTime;
        private ulong _frameStartTickCount;
        
        private readonly IDictionary<ushort, Breakpoint> _breakpoints = new Dictionary<ushort, Breakpoint>();

        public GameBoyCpu(GameBoy device, IClock clock)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _disassembler = new Z80Disassembler(device.Memory);

            Registers = new RegisterBank();
            Alu = new GameBoyAlu(Registers);
            EnableFrameLimit = true;
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));

            Clock.Tick += ClockOnTick; 
            new Thread(CpuLoop)
            {
                Name = "Z80CPULOOP",
                IsBackground = true
            }.Start();
        }

        private void ClockOnTick(object sender, EventArgs e)
        {
            _frameStartSignal.Set();
            var time = DateTime.Now.TimeOfDay;
            var delta = time - _frameStartTime;
            CyclesPerSecond = (_ticks - _frameStartTickCount) / delta.TotalSeconds;
            FramesPerSecond = 1 / delta.TotalSeconds;
            _frameStartTime = time;
            _frameStartTickCount = _ticks;
        }

        /// <summary>
        /// Gets the register bank of the processor.
        /// </summary>
        public RegisterBank Registers
        {
            get;
        }

        internal GameBoyAlu Alu
        {
            get;
        }

        /// <summary>
        /// Gets the amount of cycles the processor has executed.
        /// </summary>
        public ulong TickCount
        {
            get { return _ticks; }
        }

        /// <summary>
        /// Gets a value indicating whether the processor is active.
        /// </summary>
        public bool Running
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the processor should limit the execution speed to the original GameBoy clock speed. 
        /// Disable this if experiencing heavy performance losses.
        /// </summary>
        public bool EnableFrameLimit
        {
            get;
            set;
        }

        public double FramesPerSecond
        {
            get;
            private set;
        }

        public double CyclesPerSecond
        {
            get;
            private set;
        }

        public double SpeedFactor
        {
            get { return CyclesPerSecond / (OfficialClockFrequency * (DoubleSpeed ? 2 : 1)); }
        }

        public bool DoubleSpeed
        {
            get;
            internal set;
        }

        public bool IsPreparingSpeedSwitch
        {
            get;
            internal set;
        }
        
        public IClock Clock
        {
            get;
        }

        public void Initialize()
        {
        }

        public void Reset()
        {            
            Registers.A = _device.GbcMode ? (byte) 0x11 : (byte) 0x01;
            Registers.F = 0xB0;
            Registers.BC = 0x0013;
            Registers.DE = 0x00D8;
            Registers.HL = 0x014D;
            Registers.PC = 0x100;
            Registers.SP = 0xFFFE;
            Registers.IE = 0;
            Registers.IF = (InterruptFlags) 0xE1;
            Registers.IME = false;
        }

        public void Shutdown()
        {
            Terminate();
        }

        private void CpuLoop()
        {
            bool enabled = true;
            while (enabled)
            {
                if (WaitHandle.WaitAny(new WaitHandle[] { _continueSignal, _terminateSignal }) == 1)
                {
                    enabled = false;
                }
                else
                {
                    Running = true;
                    _continueSignal.Reset();
                    OnResumed();

                    int cycles = 0;
                    do
                    {
                        cycles += CpuStep();
                        if (cycles >= GameBoyGpu.FullFrameCycles * (DoubleSpeed ? 2 : 1))
                        {
                            _device.Spu.SpuStep(cycles / (DoubleSpeed ? 2 : 1));
                            cycles -= GameBoyGpu.FullFrameCycles * (DoubleSpeed ? 2 : 1);
                            if (EnableFrameLimit)
                            {
                                WaitHandle.WaitAny(new WaitHandle[] { _breakSignal, _frameStartSignal });
                                _frameStartSignal.Reset();
                            }
                        }

                        if (_breakpoints.TryGetValue(Registers.PC, out var breakpoint) && breakpoint.Condition(this))
                            _break = true;

                    } while (!_break);

                    _breakSignal.Reset();
                    Running = false;
                    OnPaused();
                }
            }
            OnTerminated();
        }

        private int CpuStep()
        {
            Registers.IMESet = false;

            int cycles;
            if (_halt)
            {
                cycles = 4;
            }
            else
            {
                // Execute the next instruction.
                var nextInstruction = ReadNextInstruction();
                cycles = nextInstruction.Execute(_device);
            }

            // Check for interrupts.
            bool interrupted = false;
            
            if (Registers.IE != InterruptFlags.None
                && Registers.IF != (InterruptFlags) 0xE0)
            {
                byte firedAndEnabled = (byte) (Registers.IE & Registers.IF);
                for (int i = 0; i < 5 && !interrupted; i++)
                {
                    if ((firedAndEnabled & (1 << i)) == (1 << i))
                    {
                        if (Registers.IME && !Registers.IMESet)
                        {
                            Registers.IF &= (InterruptFlags) ~(1u << i);
                            Registers.IME = false;
                            interrupted = true;
                            Rst((byte) (0x40 + (i << 3)));
                            cycles += 12;
                        }

                        _halt = false;
                    }
                }
            }

            // Update cycle dependent components.
            OnPerformedStep(new StepEventArgs(cycles / (DoubleSpeed ? 2 : 1)));

            _ticks = (_ticks + (ulong) cycles) & long.MaxValue;
            return cycles;
        }

        public void Step()
        {
            Clock.Stop();
            _break = true;
            _continueSignal.Set();
        }

        public void Run()
        {
            _frameStartTime = DateTime.Now.TimeOfDay;
            Clock.Start();
            _break = false;
            _continueSignal.Set();
        }

        public void Break()
        {
            _breakSignal.Set();
            Clock.Stop();
            _continueSignal.Reset();
            _break = true;
        }

        public void Terminate()
        {
            Clock.Stop();
            _continueSignal.Reset();
            _terminateSignal.Set();
        }

        public Breakpoint SetBreakpoint(ushort address)
        {
            if (!_breakpoints.TryGetValue(address, out var breakpoint))
            {
                breakpoint = new Breakpoint(address);
                _breakpoints.Add(address, breakpoint);
            }

            return breakpoint;
        }

        public void RemoveBreakpoint(ushort address)
        {
            _breakpoints.Remove(address);
        }

        public IEnumerable<Breakpoint> GetBreakpoints()
        {
            return _breakpoints.Values;
        }

        public Breakpoint GetBreakpointAtAddress(ushort address)
        {
            _breakpoints.TryGetValue(address, out var breakpoint);
            return breakpoint;
        }

        public void ClearBreakpoints()
        {
            _breakpoints.Clear();
        }
        
        protected virtual void OnResumed()
        {
            Resumed?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnPaused()
        {
            Paused?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnTerminated()
        {
            Terminated?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnPerformedStep(StepEventArgs args)
        {
            PerformedStep?.Invoke(this, args);
        }

        private Z80Instruction ReadNextInstruction()
        {
            _disassembler.Position = Registers.PC;
            var instruction = _disassembler.ReadNextInstruction();
            Registers.PC = _disassembler.Position;
            return instruction;
        }

        internal void Push(ushort value)
        {
            Registers.SP -= 2;
            _device.Memory.WriteUInt16(Registers.SP, value);
        }

        internal ushort Pop()
        {
            ushort value = _device.Memory.ReadUInt16(Registers.SP);
            Registers.SP += 2;
            return value;
        }

        internal void Jump(ushort address)
        {
            Registers.PC = address;
        }

        internal int JumpFlag(Z80OpCode opcode, ushort address, bool flag)
        {
            if (flag)
            {
                Jump(address);
                return opcode.ClockCycles;
            }
            return opcode.ClockCyclesAlt;
        }

        internal void Call(ushort address)
        {
            Push(Registers.PC);
            Registers.PC = address;
        }

        internal int CallFlag(Z80OpCode opcode, ushort address, bool flag)
        {
            if (flag)
            {
                Call(address);
                return opcode.ClockCycles;
            }
            return opcode.ClockCyclesAlt;
        }

        internal void Ret()
        {
            Registers.PC = Pop();
        }

        internal int RetFlag(Z80OpCode opcode, bool flag)
        {
            if (flag)
            {
                Ret();
                return opcode.ClockCycles;
            }
            return opcode.ClockCyclesAlt;
        }

        internal void Rst(byte isr)
        {
            Call(isr);
        }

        internal void Halt()
        {
            _halt = true;
        }

        internal void Stop()
        {
            if (IsPreparingSpeedSwitch)
            {
                IsPreparingSpeedSwitch = false;
                DoubleSpeed = !DoubleSpeed;
            }
        }
    }
}
