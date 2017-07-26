using System;

namespace Emux.GameBoy.Cartridge
{
    public class MemoryBankController2 : IMemoryBankController
    {
        private readonly IFullyAccessibleCartridge _cartridge;
        private readonly byte[] _romBank = new byte[0x4000];
        private readonly byte[] _ram = new byte[0x200];
        private bool _ramEnabled = false;
        private int _romBankIndex = 0;

        public MemoryBankController2(IFullyAccessibleCartridge cartridge)
        {
            if (cartridge == null)
                throw new ArgumentNullException(nameof(cartridge));
            _cartridge = cartridge;
        }

        public byte ReadByte(ushort address)
        {
            if (address < 0x4000)
                return _cartridge.ReadFromAbsoluteAddress(address);
            if (address < 0x8000)
                return _romBank[address - 0x4000];
            if (_ramEnabled && address < 0xA200)
                return _ram[address - 0xA000];
            return 0;
        }

        public void ReadBytes(ushort address, byte[] buffer, int bufferOffset, int length)
        {
            if (address < 0x4000)
                _cartridge.ReadFromAbsoluteAddress(address, buffer, bufferOffset, length);
            else if (address < 0x8000)
                Buffer.BlockCopy(_romBank, address - 0x4000, buffer, bufferOffset, length);
            else if (_ramEnabled && address < 0xA200)
                Buffer.BlockCopy(_ram, address - 0xA000, buffer, bufferOffset, length);
        }

        public void WriteByte(ushort address, byte value)
        {
            if (address < 0x2000 && (address & 0x0100) == 0)
                _ramEnabled = (value & 0xF) == 0xA;
            else if (address < 0x4000 && (address & 0x0100) == 0x0100)
                SwitchRomBank(value & 0b1111);
            else if (_ramEnabled && address >= 0xA000 && address < 0xA200)
                _ram[address - 0xA000] = (byte) (value & 0b1111);
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
