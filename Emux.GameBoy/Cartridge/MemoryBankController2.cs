using System;

namespace Emux.GameBoy.Cartridge
{
    public class MemoryBankController2 : IMemoryBankController
    {
        private readonly IFullyAccessibleCartridge _cartridge;
        private readonly byte[] _romBank = new byte[0x4000];
        private int _romBankIndex = 0;

        public MemoryBankController2(IFullyAccessibleCartridge cartridge)
        {
            if (cartridge == null)
                throw new ArgumentNullException(nameof(cartridge));
            _cartridge = cartridge;
        }

        public void Initialize()
        {
            Reset();
        }

        public void Reset()
        {
            SwitchRomBank(1);
        }

        public void Shutdown()
        {
        }

        public byte ReadByte(ushort address)
        {
            if (address < 0x4000)
                return _cartridge.ReadFromAbsoluteAddress(address);
            if (address < 0x8000)
                return _romBank[address - 0x4000];
            if (_cartridge.ExternalMemory.IsActive && address < 0xA200)
                return _cartridge.ExternalMemory.ReadByte(address - 0xA000);
            return 0;
        }

        public void ReadBytes(ushort address, byte[] buffer, int bufferOffset, int length)
        {
            if (address < 0x4000)
                _cartridge.ReadFromAbsoluteAddress(address, buffer, bufferOffset, length);
            else if (address < 0x8000)
                Buffer.BlockCopy(_romBank, address - 0x4000, buffer, bufferOffset, length);
            else if (_cartridge.ExternalMemory.IsActive && address < 0xA200)
                _cartridge.ExternalMemory.ReadBytes(address - 0xA000, buffer, bufferOffset, length);
        }

        public void WriteByte(ushort address, byte value)
        {
            if (address < 0x2000 && (address & 0x0100) == 0)
            {
                if ((value & 0xF) == 0xA)
                    _cartridge.ExternalMemory.Activate();
                else
                    _cartridge.ExternalMemory.Deactivate();
            }
            else if (address < 0x4000 && (address & 0x0100) == 0x0100)
                SwitchRomBank(value & 0b1111);
            else if (_cartridge.ExternalMemory.IsActive && address >= 0xA000 && address < 0xA200)
                _cartridge.ExternalMemory.WriteByte(address - 0xA000, (byte) (value & 0b1111));
        }

        private void SwitchRomBank(int index)
        {
            if (index == 0)
                index++;
            if (_romBankIndex != index)
            {
                _romBankIndex = index;
                _cartridge.ReadFromAbsoluteAddress(_romBank.Length * index, _romBank, 0, _romBank.Length);
            }
        }
    }
}
