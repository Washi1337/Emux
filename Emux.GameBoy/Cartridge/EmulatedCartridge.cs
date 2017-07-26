using System;
using System.Text;

namespace Emux.GameBoy.Cartridge
{
    /// <summary>
    /// Represents an emulated cartridge initialized from a byte array.
    /// </summary>
    public class EmulatedCartridge : IFullyAccessibleCartridge
    {
        private readonly byte[] _romContents;

        public EmulatedCartridge(byte[] romContents)
        {
            if (romContents == null)
                throw new ArgumentNullException(nameof(romContents));
            _romContents = romContents;

            if (CartridgeType.IsRom()) 
                BankController = new RomOnlyBankController(this);
            else if (CartridgeType.IsMbc1())
                BankController = new MemoryBankController1(this);
            else if (CartridgeType.IsMbc2())
                BankController = new MemoryBankController2(this);
            else
                throw new NotSupportedException("Unsupported cartridge type " + CartridgeType + ".");
        }

        public byte[] NintendoLogo
        {
            get
            {
                byte[] logo = new byte[0x133 - 0x104];
                Buffer.BlockCopy(_romContents, 0x104, logo, 0, logo.Length);
                return logo;
            }
        }

        public string GameTitle
        {
            get
            {
                byte[] nameBytes = new byte[16];
                Buffer.BlockCopy(_romContents, 0x134, nameBytes, 0, nameBytes.Length);
                return Encoding.ASCII.GetString(nameBytes);
            }
        }

        public byte[] NewPublisherCode
        {
            get { return new[] { _romContents[0x144], _romContents[0x145] }; }
        }

        public bool SuperGameBoyMode
        {
            get { return _romContents[0x146] == 0x3; }
        }

        public CartridgeType CartridgeType
        {
            get { return (CartridgeType) _romContents[0x147]; }
        }

        public int RomSize
        {
            get { return 0x8000 << _romContents[0x148]; }
        }

        public int ExternalRamSize
        {
            get {
                switch (_romContents[0x149])
                {
                    case 1:
                        return 0x800;
                    case 2:
                        return 0x2000;
                    case 3:
                        return 0x8000;
                }
                return 0;
            }
        }

        public bool IsJapanese
        {
            get { return _romContents[0x14B] == 0; }
        }

        public byte OldPublisherCode
        {
            get { return _romContents[0x14C]; }
        }

        public byte HeaderChecksum
        {
            get { return _romContents[0x14D]; }
        }

        public byte[] GlobalChecksum
        {
            get { return new[] { _romContents[0x14E], _romContents[0x14F] }; }
        }

        public IMemoryBankController BankController
        {
            get;
        }
        
        public byte ReadByte(ushort address)
        {
            return BankController.ReadByte(address);
        }

        public void ReadBytes(ushort address, byte[] buffer, int bufferOffset, int length)
        {
            BankController.ReadBytes(address, buffer, bufferOffset, length);
        }

        public void WriteByte(ushort address, byte value)
        {
            BankController.WriteByte(address, value);
        }

        public byte ReadFromAbsoluteAddress(int address)
        {
            return _romContents[address];
        }

        public void ReadFromAbsoluteAddress(int address, byte[] buffer, int bufferOffset, int length)
        {
            Buffer.BlockCopy(_romContents, address, buffer, bufferOffset, length);
        }
    }
}
