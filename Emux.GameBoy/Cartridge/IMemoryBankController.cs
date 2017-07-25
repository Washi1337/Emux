namespace Emux.GameBoy.Cartridge
{
    public interface IMemoryBankController
    {
        byte ReadByte(ushort address);

        void ReadBytes(ushort address, byte[] buffer, int bufferOffset, int length);

        void WriteByte(ushort address, byte value);
    }
}
