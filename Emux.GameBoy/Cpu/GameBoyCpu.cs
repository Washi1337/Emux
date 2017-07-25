using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Emux.GameBoy.Cpu
{
    public class GameBoyCpu
    {
        public const int VerticalBlankIsr = 0x0040;
        public const int LcdStatusIsr = 0x0048;
        public const int TimerOverflowIsr = 0x0050;
        public const int SerialLinkIsr = 0x0058;
        public const int JoypadPressIsr = 0x0060;
                                                  
        public const double OfficialClockSpeed = 4.194304 * 1000000;


        public event EventHandler Paused;
        public event EventHandler Terminated;

        private readonly GameBoy _device;
        private ulong _ticks;
        private readonly ManualResetEvent _continue = new ManualResetEvent(false);
        private readonly ManualResetEvent _terminate = new ManualResetEvent(false);
        private bool _break = true;
        private bool _halt = false;
        
        public GameBoyCpu(GameBoy device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            _device = device;
            Registers = new RegisterBank();
            Alu = new GameBoyAlu(Registers);
            Breakpoints = new HashSet<ushort>();
            new Thread(CpuLoop)
            {
                Name = "Z80CPULOOP",
                IsBackground = true
            }.Start();
        }

        public RegisterBank Registers
        {
            get;
        }

        internal GameBoyAlu Alu
        {
            get;
        }

        public ulong TickCount
        {
            get { return _ticks; }
        }

        public bool Running
        {
            get;
            private set;
        }

        public ISet<ushort> Breakpoints
        {
            get;
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

                    do
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
                        if (Registers.IME && !Registers.IMESet && Registers.IE != InterruptFlags.None && Registers.IF != (InterruptFlags) 0xE0)
                        {
                            byte firedAndEnabled = (byte)(Registers.IE & Registers.IF);
                            for (int i = 0; i < 5 && !interrupted; i++)
                            {
                                if ((firedAndEnabled & (1 << i)) == (1 << i))
                                {
                                    Registers.IF &= (InterruptFlags)~(1u << i);
                                    Registers.IME = false;
                                    interrupted = true;
                                    Rst((byte)(0x40 + (i << 3)));
                                    cycles += 12;
                                    _halt = false;
                                }
                            }
                        }
                        
                        _device.Gpu.Update(cycles);
                        _ticks = (_ticks + (ulong)cycles) & long.MaxValue;

                        if (Breakpoints.Contains(Registers.PC))
                            _break = true;

                    } while (!_break);

                    Running = false;
                    OnPaused();
                }
            }
            OnTerminated();
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
            ushort offset = Registers.PC;
            byte code = _device.Memory.ReadByte(Registers.PC++);

            var opcode = code != 0xCB
                ? Z80OpCodes.SingleByteOpCodes[code]
                : Z80OpCodes.PrefixedOpCodes[_device.Memory.ReadByte(Registers.PC++)];

            byte[] operand = _device.Memory.ReadBytes(Registers.PC, opcode.OperandLength);
            Registers.PC += (ushort) operand.Length;

            var instruction = new Z80Instruction(offset, opcode, operand);
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
