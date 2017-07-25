namespace Emux.GameBoy.Cartridge
{
    public interface ICartridge
    {
        byte[] NintendoLogo { get; }
        string GameTitle { get; }
        byte[] NewPublisherCode { get; }
        bool SuperGameBoyMode { get; }
        CartridgeType CartridgeType { get; }
        int RomSize { get; }
        int ExternalRamSize { get; }
        bool IsJapanese { get; }
        byte OldPublisherCode { get; }
        byte HeaderChecksum { get; }
        byte[] GlobalChecksum { get; }

        byte ReadByte(ushort address);

        void ReadBytes(ushort address, byte[] buffer, int bufferOffset, int length);

        void WriteByte(ushort address, byte value);
    }

    public interface IFullyAccessibleCartridge : ICartridge
    {
        byte ReadFromAbsoluteAddress(int address);

        void ReadFromAbsoluteAddress(int address, byte[] buffer, int bufferOffset, int length);


    }
}