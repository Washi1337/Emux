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
		public const int InterruptStartAddr = 0x40;
        public const int VerticalBlankIsr = 0x0040;
        public const int LcdStatusIsr = 0x0048;
        public const int TimerOverflowIsr = 0x0050;
        public const int SerialLinkIsr = 0x0058;
        public const int JoypadPressIsr = 0x0060;
        public const double OfficialClockFrequency = 4194304;
        
        private readonly Z80Disassembler _disassembler;
        private readonly GameBoy _device;

		public double CyclesPerSecond;

		private ulong _frameStartTickCount;
		private ulong _ticks;
        public bool IsBroken = true;
        public bool Halted = false;
        private readonly Z80Instruction _readInstruction = new Z80Instruction(0, Z80OpCodes.SingleByteOpCodes[0], new byte[0]);

        public Z80Instruction LastInstruction { get; private set; }

        public GameBoyCpu(GameBoy device, IClock clock)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _disassembler = new Z80Disassembler(device.Memory);

            Registers = new RegisterBank();
            Alu = new GameBoyAlu(Registers);
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
            set;
        }

		public bool DoubleSpeed
        {
            get;
            internal set;
        }

		public int SpeedMultiplier => DoubleSpeed ? 2 : 1;

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

        }


        internal int PerformNextInstruction()
        {
            Registers.IMESet = false;

            int cycles;
            if (Halted)
            {
                cycles = 4;
            }
            else
            {
                // Execute the next instruction.
                Z80Instruction nextInstruction = null;
                do
                {
                    nextInstruction = ReadNextInstruction();
                } while (nextInstruction == null || nextInstruction.OpCode.ClockCycles == 0);
                cycles = nextInstruction.Execute(_device);
                LastInstruction = nextInstruction;
            }

            // Check for interrupts.
            bool interrupted = false;
            
            var firedAndEnabled = (byte)(Registers.IE & Registers.IF);
            if (firedAndEnabled != 0)
            {
				var vector = InterruptVector.VBlank;
                for (int i = 0; i < 5 && !interrupted; i++)
                {
					var bit = 1 << i;
                    if ((firedAndEnabled & bit) == bit)
                    {
                        if (Registers.IME && !Registers.IMESet)
                        {
                            Registers.IF &= (InterruptFlags)~bit;
                            Registers.IME = false;
                            interrupted = true;
                            cycles += Rst(vector);
                        }

                        Halted = false;
                    }

					vector += 0x08;
                }
            }

            _ticks = (_ticks + (ulong) cycles) & long.MaxValue;

			return cycles;
        }

		public void SecondElapsed(TimeSpan delta)
		{
			CyclesPerSecond = (_ticks - _frameStartTickCount) / delta.TotalSeconds;
			_ticks = 0;
		}

        private Z80Instruction ReadNextInstruction()
        {
            _disassembler.ReadInstruction(ref Registers.PC, _readInstruction);
            return _readInstruction;
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

        internal int Rst(InterruptVector isr)
        {
            Call((ushort)isr);
			return 12;
        }

        internal void Halt()
        {
            Halted = true;
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
