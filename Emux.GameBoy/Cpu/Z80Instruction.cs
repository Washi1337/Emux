using System;

namespace Emux.GameBoy.Cpu
{
    public class Z80Instruction
    {
        public Z80Instruction(ushort offset, Z80OpCode opCode, byte[] operand)
        {
            Offset = offset;
            OpCode = opCode;
            RawOperand = operand;
        }

        public Z80Instruction(Z80OpCode opCode, byte[] operand)
        {
            OpCode = opCode;
            RawOperand = operand;
        }

        public ushort Offset
        {
            get;
        }

        public Z80OpCode OpCode
        {
            get;
        }

        public byte[] RawOperand
        {
            get;
        }

        public byte Operand8
        {
            get { return RawOperand[0]; }
        }

        public ushort Operand16
        {
            get { return BitConverter.ToUInt16(RawOperand, 0); }
        }

        public override string ToString()
        {
            switch (RawOperand.Length)
            {
                default:
                    return Offset.ToString("X4") + ": " + OpCode.Disassembly;
                case 1:
                    return Offset.ToString("X4") + ": " + string.Format(OpCode.Disassembly, Operand8);
                case 2:
                    return Offset.ToString("X4") + ": " + string.Format(OpCode.Disassembly, Operand16);
            }
        }

        public int Execute(GameBoy device)
        {
            return OpCode.Operation(device, this);
        }
    }
}
