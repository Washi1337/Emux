using System.IO;
using Emux.GameBoy.Memory;

namespace Emux.GameBoy.Cpu
{
    public class Z80Disassembler 
    {
        private readonly GameBoyMemory _memory;

        public Z80Disassembler(GameBoyMemory memory)
        {
            _memory = memory;
        }

        public ushort Position
        {
            get;
            set;
        }

        public Z80Instruction ReadNextInstruction()
        {
            ushort offset = Position;
            byte code = _memory.ReadByte(Position++);

            var opcode = code != 0xCB
                ? Z80OpCodes.SingleByteOpCodes[code]
                : Z80OpCodes.PrefixedOpCodes[_memory.ReadByte(Position++)];

            byte[] operand = _memory.ReadBytes(Position, opcode.OperandLength);
            Position += (ushort) operand.Length;

            var instruction = new Z80Instruction(offset, opcode, operand);
            return instruction;
        }
    }
}
