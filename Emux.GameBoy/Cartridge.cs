using System;
using System.IO;
using System.Text;

namespace Emux.GameBoy
{
    public class Cartridge
    {
        private readonly byte[] _romContents;

        public Cartridge(byte[] romContents)
        {
            if (romContents == null)
                throw new ArgumentNullException(nameof(romContents));
            _romContents = romContents;
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

        public bool SuperGameBoyMode
        {
            get { return _romContents[0x146] == 0x3; }
        }



        public byte[] RomContents
        {
            get { return _romContents; }
        }
    }
}
