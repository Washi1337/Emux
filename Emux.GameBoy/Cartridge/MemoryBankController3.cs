using System;

namespace Emux.GameBoy.Cartridge
{
    public class MemoryBankController3 : IMemoryBankController
    {
        private readonly IFullyAccessibleCartridge _cartridge;
        private readonly byte[] _romBank = new byte[0x4000];
        private int _romBankIndex = 0;
        private int _ramBankOrRtcIndex;
        private readonly byte[] _externalRam = new byte[0x8000];
        private bool _ramRtcEnabled = false;
        private readonly byte[] _rtc = new byte[5];

        public MemoryBankController3(IFullyAccessibleCartridge cartridge)
        {
            if (cartridge == null)
                throw new ArgumentNullException(nameof(cartridge));
            _cartridge = cartridge;
            SwitchRomBank(1);
        }

        public byte ReadByte(ushort address)
        {
            if (address < 0x4000)
                return _cartridge.ReadFromAbsoluteAddress(address);
            if (address < 0x8000)
                return _romBank[address - 0x4000];
            if (_ramRtcEnabled && address >= 0xA000 && address <= 0xBFFF)
                return ReadRamOrRtc(address);
            return 0;
        }

        private byte ReadRamOrRtc(ushort address)
        {
            return _ramBankOrRtcIndex <= 3
                ? (_cartridge.CartridgeType.HasRam()
                    ? _externalRam[address - 0xA000 + GetRamOffset()]
                    : (byte) 0)
                : (_cartridge.CartridgeType.HasTimer()
                    ? _rtc[_ramBankOrRtcIndex - 0x8]
                    : (byte) 0);
        }

        public void ReadBytes(ushort address, byte[] buffer, int bufferOffset, int length)
        {
            if (address < 0x4000)
                _cartridge.ReadFromAbsoluteAddress(address, buffer, bufferOffset, length);
            else if (address < 0x8000)
                Buffer.BlockCopy(_romBank, address - 0x4000, buffer, bufferOffset, length);
            if (_ramRtcEnabled && address >= 0xA000 && address <= 0xBFFF)
                Buffer.BlockCopy(_externalRam, address - 0xA000 + GetRamOffset(), buffer, bufferOffset, length);
        }

        public void WriteByte(ushort address, byte value)
        {
            if (address < 0x2000)
                _ramRtcEnabled = (value & 0xA) == 0xA;
            else if (address < 0x4000)
                SwitchRomBank(value & 0x1F);
            else if (address < 0x6000)
                _ramBankOrRtcIndex = value & 3;
            else if (address < 0x8000)
            {
                // TODO: latch clock data
            }
            else if (_ramRtcEnabled && address >= 0xA000 && address - 0xA000 < _externalRam.Length)
                _externalRam[address - 0xA000 + GetRamOffset()] = value;
        }
        
        private void SwitchRomBank(int index)
        {
            if (_romBankIndex != index)
            {
                if (index == 0)
                    index++;
                _romBankIndex = index & 0x7F;
                UpdateRomBank();
            }
        }

        private void UpdateRomBank()
        {
            _cartridge.ReadFromAbsoluteAddress(_romBank.Length * _romBankIndex, _romBank, 0, _romBank.Length);
        }

        private int GetRamOffset()
        {
            return _ramBankOrRtcIndex * 0x2000;
        }
        
    }
}