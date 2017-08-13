using Emux.GameBoy.Memory;

namespace Emux.GameBoy.Cpu
{
    /// <summary>
    /// Provides a mechanism for reading Z80 instructions from the memory of a GameBoy device.
    /// </summary>
    public class Z80Disassembler 
    {
        private readonly GameBoyMemory _memory;
        private ushort _position;

        public Z80Disassembler(GameBoyMemory memory)
        {
            _memory = memory;
        }

        /// <summary>
        /// Gets or sets the position of the disassembler to read the next instruction from.
        /// </summary>
        public ushort Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// Reads the next instruction from memory.
        /// </summary>
        /// <returns>The disassembled instruction.</returns>
        public Z80Instruction ReadNextInstruction()
        {
            ushort offset = _position;
            byte code = _memory.ReadByte(_position++);

            var opcode = code != 0xCB
                ? Z80OpCodes.SingleByteOpCodes[code]
                : Z80OpCodes.PrefixedOpCodes[_memory.ReadByte(_position++)];

            byte[] operand = _memory.ReadBytes(_position, opcode.OperandLength);
            _position += (ushort) operand.Length;

            var instruction = new Z80Instruction(offset, opcode, operand);
            return instruction;
        }
    }
}
