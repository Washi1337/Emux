using System;

namespace Emux.GameBoy.Cartridge
{
    public class RomOnlyBankController : IMemoryBankController
    {
        private readonly IFullyAccessibleCartridge _cartridge;
        private readonly byte[] _ramBank;
        private bool _ramEnabled = false;

        public RomOnlyBankController(IFullyAccessibleCartridge cartridge)
        {
            if (cartridge == null)
                throw new ArgumentNullException(nameof(cartridge));
            _cartridge = cartridge;

            if (cartridge.CartridgeType.HasRam())
                _ramBank = new byte[0x2000];
        }

        public byte ReadByte(ushort address)
        {
            if (address < 0x8000)
                return _cartridge.ReadFromAbsoluteAddress(address);
            if (_ramEnabled && address >= 0xA000 && address <= 0xBFFF)
                return _ramBank[address - 0xA000];
            return 0;
        }

        public void ReadBytes(ushort address, byte[] buffer, int bufferOffset, int length)
        {
            if (address < 0x8000)
                _cartridge.ReadFromAbsoluteAddress(address, buffer, bufferOffset, length);
            if (_ramEnabled && address >= 0xA000 && address <= 0xBFFF)
                Buffer.BlockCopy(_ramBank, address - 0xA000, buffer, bufferOffset, length);
        }

        public void WriteByte(ushort address, byte value)
        {
            // TODO: ram enable
        }
    }
}
