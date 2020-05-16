using Emux.GameBoy.Memory;

namespace Emux.GameBoy.Cpu
{
    /// <summary>
    /// Provides a mechanism for reading Z80 instructions from the memory of a GameBoy device.
    /// </summary>
    public class Z80Disassembler 
    {
        private readonly GameBoyMemory _memory;

        public Z80Disassembler(GameBoyMemory memory)
        {
            _memory = memory;
        }

		/// <summary>
		/// Reads the next instruction from memory.
		/// </summary>
		/// <returns>The disassembled instruction.</returns>
		public void ReadInstruction(ref ushort location, Z80Instruction outInstruction)
        {
            ushort offset = location;
            byte code = _memory.ReadByte(location++);

            var opcode = code != Z80OpCodes.ExtendedTableOpcode
                ? Z80OpCodes.SingleByteOpCodes[code]
                : Z80OpCodes.PrefixedOpCodes[_memory.ReadByte(location++)];

            var operands = _memory.ReadBytes(location, opcode.OperandLength);
            location += (ushort)operands.Length;

            outInstruction.Set(offset, opcode, operands);
        }
    }
}
