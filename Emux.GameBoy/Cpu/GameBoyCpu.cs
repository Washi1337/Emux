using System;
using System.Collections.Generic;
using System.Threading;

namespace Emux.GameBoy.Cpu
{
    /// <summary>
    /// Represents a central processing unit of a GameBoy device.
    /// </summary>
    public class GameBoyCpu
    {
        public const int VerticalBlankIsr = 0x0040;
        public const int LcdStatusIsr = 0x0048;
        public const int TimerOverflowIsr = 0x0050;
        public const int SerialLinkIsr = 0x0058;
        public const int JoypadPressIsr = 0x0060;
        public const double OfficialClockFrequency = 4194304;
        
        /// <summary>
        /// Occurs when the processor is paused by breaking the execution explicitly, or when the control flow hit a breakpoint.
        /// </summary>
        public event EventHandler Paused;

        /// <summary>
        /// Occurs when the process has completely shut down.
        /// </summary>
        public event EventHandler Terminated;

        private readonly Z80Disassembler _disassembler;
        private readonly GameBoy _device;
        private readonly ManualResetEvent _continue = new ManualResetEvent(false);
        private readonly ManualResetEvent _terminate = new ManualResetEvent(false);
        private ulong _ticks;
        private bool _break = true;
        private bool _halt = false;
        
        public GameBoyCpu(GameBoy device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            _device = device;
            _disassembler = new Z80Disassembler(device.Memory);

            Registers = new RegisterBank();
            Alu = new GameBoyAlu(Registers);
            Breakpoints = new HashSet<ushort>();
            FrameLimit = true;

            new Thread(CpuLoop)
            {
                Name = "Z80CPULOOP",
                IsBackground = true
            }.Start();
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
        /// Gets a collection of memory addresses to break the execution on.
        /// </summary>
        public ISet<ushort> Breakpoints
        {
            get;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the processor should limit the execution speed to the original GameBoy clock speed.
        /// </summary>
        public bool FrameLimit
        {
            get;
            set;
        }

        private void CpuLoop()
        {
            bool enabled = true;
            while (enabled)
            {
                if (WaitHandle.WaitAny(new WaitHandle[] { _continue, _terminate }) == 1)
                {
                    enabled = false;
                }
                else
                {
                    Running = true;
                    _continue.Reset();
                    
                    var frameStart = DateTime.Now.TimeOfDay;
                    var start = frameStart;

                    int cycles = 0;
                    int frames = 0;
                    do
                    {
                        cycles += CpuStep();
                        if (cycles >= 70224)
                        {
                            var end = DateTime.Now.TimeOfDay;
                            var delta = (end - frameStart);

                            if (FrameLimit && delta.TotalMilliseconds < 1000.0 / 59.73)
                                Thread.Sleep((int)(1000.0 / 59.73));

                            frameStart = end;
                            cycles -= 70224;
                            frames++;

                            if ((end - start).TotalSeconds >= 1)
                            {
                                start = end;
                                Console.WriteLine(frames);
                                frames = 0;
                            }
                        }

                        if (Breakpoints.Contains(Registers.PC))
                            _break = true;

                    } while (!_break);

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
            if (Registers.IME && !Registers.IMESet 
                && Registers.IE != InterruptFlags.None
                && Registers.IF != (InterruptFlags) 0xE0)
            {
                byte firedAndEnabled = (byte) (Registers.IE & Registers.IF);
                for (int i = 0; i < 5 && !interrupted; i++)
                {
                    if ((firedAndEnabled & (1 << i)) == (1 << i))
                    {
                        Registers.IF &= (InterruptFlags) ~(1u << i);
                        Registers.IME = false;
                        interrupted = true;
                        Rst((byte) (0x40 + (i << 3)));
                        cycles += 12;
                        _halt = false;
                    }
                }
            }

            // Update cycle dependent components.
            _device.Gpu.GpuStep(cycles);
            _device.Timer.TimerStep(cycles);

            _ticks = (_ticks + (ulong) cycles) & long.MaxValue;
            return cycles;
        }

        public void Step()
        {
            _break = true;
            _continue.Set();
        }

        public void Run()
        {
            _break = false;
            _continue.Set();
        }

        public void Break()
        {
            _continue.Reset();
            _break = true;
        }

        public void Terminate()
        {
            _continue.Reset();
            _terminate.Set();
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
        
        protected virtual void OnPaused()
        {
            Paused?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnTerminated()
        {
            Terminated?.Invoke(this, EventArgs.Empty);
        }
    }
}
