using Emux.GameBoy.Memory;

namespace Emux.GameBoy.Cpu
{
    /// <summary>
    /// Provides a mechanism for reading Z80 instructions from the memory of a GameBoy device.
    /// </summary>
    public class Z80Disassembler 
    {
        private readonly GameBoyMemory _memory;
        private readonly Z80Instruction _readInstruction = new Z80Instruction(0, Z80OpCodes.SingleByteOpCodes[0], new byte[0]);

        public Z80Disassembler(GameBoyMemory memory)
        {
            _memory = memory;
        }

		/// <summary>
		/// Gets or sets the position of the disassembler to read the next instruction from.
		/// </summary>
		public ushort Position { get; set; }

		/// <summary>
		/// Reads the next instruction from memory.
		/// </summary>
		/// <returns>The disassembled instruction.</returns>
		public Z80Instruction ReadNextInstruction()
        {
            ushort offset = Position;
            byte code = _memory.ReadByte(Position++);

            var opcode = code != Z80OpCodes.ExtendedTableOpcode
                ? Z80OpCodes.SingleByteOpCodes[code]
                : Z80OpCodes.PrefixedOpCodes[_memory.ReadByte(Position++)];

            var operands = _memory.ReadBytes(Position, opcode.OperandLength);
            Position += (ushort)operands.Length;

            _readInstruction.Set(offset, opcode, operands);
            return _readInstruction;
        }
    }
}
