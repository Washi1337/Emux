using System;

namespace Emux.GameBoy.Cartridge
{
    public class MemoryBankController5 : IMemoryBankController
    {
        private readonly IFullyAccessibleCartridge _cartridge;
        private readonly byte[] _romBank = new byte[0x4000];
        private int _romBankIndex;
        private int _ramBankIndex;

        public MemoryBankController5(IFullyAccessibleCartridge cartridge)
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
            if (_cartridge.ExternalMemory.IsActive && address >= 0xA000 && address <= 0xBFFF)
                return _cartridge.ExternalMemory.ReadByte(address - 0xA000 + GetRamOffset());
            return 0;
        }

        public void ReadBytes(ushort address, byte[] buffer, int bufferOffset, int length)
        {
            if (address < 0x4000)
                _cartridge.ReadFromAbsoluteAddress(address, buffer, bufferOffset, length);
            else if (address < 0x8000)
                Buffer.BlockCopy(_romBank, address - 0x4000, buffer, bufferOffset, length);
            else if (_cartridge.ExternalMemory.IsActive && address >= 0xA000 && address <= 0xBFFF)
                _cartridge.ExternalMemory.ReadBytes(address - 0xA000 + GetRamOffset(), buffer, bufferOffset, length);
        }

        public void WriteByte(ushort address, byte value)
        {
            if (address < 0x2000)
            {
                if ((value & 0xA) == 0xA)
                    _cartridge.ExternalMemory.Activate();
                else
                    _cartridge.ExternalMemory.Deactivate();
            }
            else if (address < 0x3000)
            {
                SwitchRomBank((_romBankIndex & 0x100) | value);
            }
            else if (address < 0x4000)
            {
                SwitchRomBank((_romBankIndex & 0xFF) | ((value & 1) << 8));
            }
            else if (address < 0x6000)
            {
                _ramBankIndex = value & 0xF;
            }
            else if (_cartridge.ExternalMemory.IsActive && address >= 0xA000 && address - 0xA000 < _cartridge.ExternalRamSize)
            {
                _cartridge.ExternalMemory.WriteByte(address - 0xA000 + GetRamOffset(), value);
            }
        }

        private void SwitchRomBank(int index)
        {
            if (_romBankIndex != index)
            {
                _romBankIndex = index;
                _cartridge.ReadFromAbsoluteAddress(_romBank.Length * _romBankIndex, _romBank, 0, _romBank.Length);
            }
        }
        
        private int GetRamOffset()
        {
            return _ramBankIndex * 0x2000;
        }
    }
}
