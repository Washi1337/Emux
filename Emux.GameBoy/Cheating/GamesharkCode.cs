using System;

namespace Emux.GameBoy.Cheating
{
    public class GamesharkCode
    {
        public GamesharkCode(byte[] code)
        {
            RawCode = new byte[4];
            Buffer.BlockCopy(code, 0, RawCode, 0, RawCode.Length);
            Enabled = true;
        }

        public byte CodeType
        {
            get { return RawCode[0]; }
        }

        public byte Value
        {
            get { return RawCode[1]; }
        }

        public ushort Address
        {
            get { return (ushort) (RawCode[2] | (RawCode[3] << 8)); }
        }

        public bool Enabled
        {
            get;
            set;
        }

        public byte[] RawCode
        {
            get;
        }

        public string Description
        {
            get;
            set;
        }
    }
}