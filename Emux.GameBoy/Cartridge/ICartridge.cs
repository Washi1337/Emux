namespace Emux.GameBoy.Cartridge
{
    /// <summary>
    /// Provides members for accessing an inserted GameBoy cartridge.
    /// </summary>
    public interface ICartridge
    {
        /// <summary>
        /// Gets the compressed Nintendo Logo bitmap used by the BIOS.
        /// </summary>
        byte[] NintendoLogo { get; }

        /// <summary>
        /// Gets the title of the game.
        /// </summary>
        string GameTitle { get; }

        /// <summary>
        /// Gets the publisher code of the cartridge. This property is used by newer cartridges only.
        /// </summary>
        byte[] NewPublisherCode { get; }

        /// <summary>
        /// Gets a value indicating whether the cartridge is designed specifically for GameBoy Color devices.
        /// </summary>
        GameBoyColorFlag GameBoyColorFlag { get; }

        /// <summary>
        /// Gets a value indicating whether Super GameBoy features are enabled or not by this cartridge. 
        /// </summary>
        bool SuperGameBoyMode { get; }

        /// <summary>
        /// Gets the type of the cartridge, including the present memory bank controller (MBC), if any.
        /// </summary>
        CartridgeType CartridgeType { get; }

        /// <summary>
        /// Gets the size in bytes of all ROM data present in the cartridge.
        /// </summary>
        int RomSize { get; }
        
        /// <summary>
        /// Gets the size in bytes of external RAM present in the cartridge, if any.
        /// </summary>
        int ExternalRamSize { get; }

        /// <summary>
        /// Gets a value indicating the cartridge was produced for the Japanese market or not.
        /// </summary>
        bool IsJapanese { get; }

        /// <summary>
        /// Gets the publisher code of the cartridge. This property is used by older cartridges only.
        /// </summary>
        byte OldPublisherCode { get; }

        /// <summary>
        /// Gets the checksum of the cartridge header.
        /// </summary>
        byte HeaderChecksum { get; }

        /// <summary>
        /// Gets the global checksum of the cartrige.
        /// </summary>
        byte[] GlobalChecksum { get; }

        /// <summary>
        /// Reads a single byte from the cartrige at a given address.
        /// </summary>
        /// <param name="address">The address to read from.</param>
        /// <returns>The byte at the given location.</returns>
        byte ReadByte(ushort address);

        /// <summary>
        /// Reads a block of bytes from the cartridge starting at a given address.
        /// </summary>
        /// <param name="address">The start address.</param>
        /// <param name="buffer">The buffer to write the bytes to.</param>
        /// <param name="bufferOffset">The destinatino offset of the buffer to write to.</param>
        /// <param name="length">The amount of bytes to read.</param>
        void ReadBytes(ushort address, byte[] buffer, int bufferOffset, int length);

        /// <summary>
        /// Writes a byte to the cartridge.
        /// </summary>
        /// <param name="address">The address to write to.</param>
        /// <param name="value">The value to write.</param>
        void WriteByte(ushort address, byte value);
    }

    /// <summary>
    /// Provides members for accessing a fully accessible cartridge.
    /// </summary>
    public interface IFullyAccessibleCartridge : ICartridge
    {
        IExternalMemory ExternalMemory
        {
            get;
        }

        /// <summary>
        /// Reads a single byte from the raw data of the cartridge.
        /// </summary>
        /// <param name="address">The absolute address of the raw data.</param>
        /// <returns>The byte at the given absolute address.</returns>
        byte ReadFromAbsoluteAddress(int address);

        /// <summary>
        /// Reads a block of bytes from the raw data of the cartridge.
        /// </summary>
        /// <param name="address">The start address.</param>
        /// <param name="buffer">The buffer to write the bytes to.</param>
        /// <param name="bufferOffset">The destinatino offset of the buffer to write to.</param>
        /// <param name="length">The amount of bytes to read.</param>
        void ReadFromAbsoluteAddress(int address, byte[] buffer, int bufferOffset, int length);
    }
}