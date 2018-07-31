using System;

namespace Emux.GameBoy.Cartridge
{
    public class MemoryBankController1 : IMemoryBankController
    {
        private readonly IFullyAccessibleCartridge _cartridge;
        private readonly byte[] _romBank = new byte[0x4000];
        private int _romBankIndex;
        private int _ramBankIndex;
        private bool _romRamMode;

        public MemoryBankController1(IFullyAccessibleCartridge cartridge)
        {
            _cartridge = cartridge ?? throw new ArgumentNullException(nameof(cartridge));
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
            if (_cartridge.ExternalMemory.IsActive && address >= 0xA000 && address <= 0xBFFF)
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
            else if (address < 0x4000)
                SwitchRomBank(value & 0x1F);
            else if (address < 0x6000)
                SwitchRamBank(value & 0x3);
            else if (address < 0x8000)
                SwitchRomRamMode(value);
            else if (_cartridge.ExternalMemory.IsActive && address >= 0xA000 && address - 0xA000 < _cartridge.ExternalRamSize)
                _cartridge.ExternalMemory.WriteByte(address - 0xA000 + GetRamOffset(), value);
        }

        private void SwitchRomRamMode(byte value)
        {
            bool romRamMode = value == 1;
            if (_romRamMode != romRamMode)
            {
                _romRamMode = romRamMode;
                UpdateRomBank();
            }
        }

        private void SwitchRamBank(int index)
        {
            if (_ramBankIndex != index)
            {
                _ramBankIndex = index;
                UpdateRomBank();
            }
        }

        private void SwitchRomBank(int index)
        {
            if (_romBankIndex != index)
            {
                if (index == 0 || index == 0x20 || index == 0x40 || index == 0x60)
                    index++;
                _romBankIndex = index;
                UpdateRomBank();
            }
        }

        private void UpdateRomBank()
        {
            int index = _romBankIndex;
            if (_romRamMode)
                index |= _ramBankIndex << 5;
            _cartridge.ReadFromAbsoluteAddress(_romBank.Length * index, _romBank, 0, _romBank.Length);
        }

        private int GetRamOffset()
        {
            return _ramBankIndex * 0x2000;
        }
    }
}
