using System;

namespace Emux.GameBoy.Cpu
{
    /// <summary>
    /// Represents an instruction in the Z80 instruction set.
    /// </summary>
    public class Z80Instruction
    {
        public Z80Instruction(ushort offset, Z80OpCode opCode, byte[] operand)
        {
            Set(offset, opCode, operand);
        }
        public void Set(ushort offset, Z80OpCode opCode, byte[] operand)
        {
            Offset = offset;
            OpCode = opCode;
            RawOperand = operand;
        }

        /// <summary>
        /// The memory address the instruction is located at.
        /// </summary>
        public ushort Offset
        {
            get;
            private set;
        }

        /// <summary>
        /// The operation code of the instructon.
        /// </summary>
        public Z80OpCode OpCode
        {
            get;
            private set;
        }

        /// <summary>
        /// The bytes that form the operand of the instruction.
        /// </summary>
        public byte[] RawOperand
        {
            get;
            private set;
        }

        /// <summary>
        /// The operand interpreted as a single unsigned 8 bit integer.
        /// </summary>
        public byte Operand8 => RawOperand[0];

        /// <summary>
        /// The operand interpreted as an unsigned 16 bit integer.
        /// </summary>
        public ushort Operand16 => BitConverter.ToUInt16(RawOperand, 0);

        /// <summary>
        /// Gets the assembler code representing the instruction.
        /// </summary>
        public string Disassembly
        {
            get
            {
                switch (RawOperand.Length)
                {
                    default:
                        return OpCode.Disassembly;
                    case 1:
                        return string.Format(OpCode.Disassembly, Operand8);
                    case 2:
                        return string.Format(OpCode.Disassembly, Operand16);
                }
            }
        }

        public override string ToString() => Offset.ToString("X4") + ": " + Disassembly;

        /// <summary>
        /// Executes the instruction on the given device.
        /// </summary>
        /// <param name="device">The device to execute the instruction on.</param>
        /// <returns>The clock cycles it took to evaluate the instruction.</returns>
        public int Execute(GameBoy device) => OpCode.Operation(device, this);
    }
}
