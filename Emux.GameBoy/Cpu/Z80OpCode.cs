using System;

namespace Emux.GameBoy.Cpu
{
    public delegate void Z80OpCodeOperation(GameBoy device, Z80Instruction z80Instruction);
    public delegate int Z80OpCodeOperationAlt(GameBoy device, Z80Instruction z80Instruction);

    public struct Z80OpCode
    {
        public static readonly Z80OpCodeOperation NotSupported = (_, i) => throw new NotSupportedException("Instruction '" + i.ToString() + "' not supported.");
        public static readonly Z80OpCodeOperation InvalidOpcode = (_, i) => throw new NotSupportedException("Invalid OpCode " + i.ToString() + ".");

        public readonly string Disassembly;

        public readonly byte Op1;
        public readonly byte Op2;
        public readonly int OperandLength;
        public readonly int ClockCycles;
        public readonly int ClockCyclesAlt;
        public readonly Z80OpCodeOperationAlt Operation;
        
        internal Z80OpCode(string disassembly, byte op1, byte op2, int operandLength, int clockCycles,
            Z80OpCodeOperation operation)
            : this(disassembly, op1, op2, operandLength, clockCycles, clockCycles, (d, i) => { operation(d, i); return clockCycles; })
        {
        }

        internal Z80OpCode(string disassembly, byte op1, byte op2, int operandLength, int clockCycles, int clockCyclesAlt, Z80OpCodeOperationAlt operation)
        {
            Disassembly = disassembly;
            Op1 = op1;
            Op2 = op2;
            OperandLength = operandLength;
            ClockCycles = clockCycles;
            ClockCyclesAlt = clockCyclesAlt;
            Operation = operation;
        }
        
    }
}
