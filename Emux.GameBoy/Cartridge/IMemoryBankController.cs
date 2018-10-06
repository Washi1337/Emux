namespace Emux.GameBoy.Cartridge
{
    /// <summary>
    /// Provides methods for emulation of a memory bank controller (MBC).
    /// </summary>
    public interface IMemoryBankController : IGameBoyComponent
    {
        byte ReadByte(ushort address);

        void ReadBytes(ushort address, byte[] buffer, int bufferOffset, int length);

        void WriteByte(ushort address, byte value);
    }
}
