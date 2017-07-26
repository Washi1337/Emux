using System;

namespace Emux.GameBoy.Cpu
{
    /// <summary>
    /// Represents a reference to a method that evaluates a Z80 instruction.
    /// </summary>
    /// <param name="device">The device to run the instruction on.</param>
    /// <param name="z80Instruction">The instruction to run.</param>
    public delegate void Z80OpCodeOperation(GameBoy device, Z80Instruction z80Instruction);

    /// <summary>
    /// Represents a reference to a method that evaluates a Z80 instruction that evaluates in a variable amount of clock cycles.
    /// </summary>
    /// <param name="device">The device to run the instruction on.</param>
    /// <param name="z80Instruction">The instruction to run.</param>
    /// <returns>The amount of clock cycles that it took to evaluate the instruction.</returns>
    public delegate int Z80OpCodeOperationAlt(GameBoy device, Z80Instruction z80Instruction);

    /// <summary>
    /// Represents a single operation code of the Z80 instruction set.
    /// </summary>
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
