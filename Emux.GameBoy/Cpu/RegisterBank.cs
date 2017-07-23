// ReSharper disable InconsistentNaming

namespace Emux.GameBoy.Cpu
{
    public class RegisterBank
    {
        public byte A;
        public byte F;
        public byte B;
        public byte C;
        public byte D;
        public byte E;
        public byte H;
        public byte L;

        public ushort PC;
        public ushort SP;

        public InterruptFlags IE;
        public InterruptFlags IF;
        public bool IME;
        public bool IMESet;

        public ushort AF
        {
            get { return (ushort)((A << 8) | F); }
            set
            {
                A = (byte)((value >> 8) & 0xFF);
                F = (byte)(value & 0xFF);
            }
        }

        public ushort BC
        {
            get { return (ushort)((B << 8) | C); }
            set
            {
                B = (byte)((value >> 8) & 0xFF);
                C = (byte)(value & 0xFF);
            }
        }

        public ushort DE
        {
            get { return (ushort)((D << 8) | E); }
            set
            {
                D = (byte)((value >> 8) & 0xFF);
                E = (byte)(value & 0xFF);
            }
        }

        public ushort HL
        {
            get { return (ushort)((H << 8) | L); }
            set
            {
                H = (byte)((value >> 8) & 0xFF);
                L = (byte)(value & 0xFF);
            }
        }

        public bool GetFlags(RegisterFlags flag)
        {
            return (F & (byte) flag) == (byte) flag;
        }

        public void OverwriteFlags(RegisterFlags newFlags)
        {
            F = (byte) newFlags;
        }

        public void SetFlags(RegisterFlags flags)
        {
            F |= (byte) flags;
        }

        public void ClearFlags(RegisterFlags flags)
        {
            unchecked
            {
                F &= (byte) ~(byte) flags;
            }
        }

        public void Reset()
        {
            A = B = C = D = E = F = H = L = 0;
            IE = IF = InterruptFlags.None;
            IME = false;
        }

        public override string ToString()
        {
            return string.Format("AF: {0:X4}\r\n" +
                                 "BC: {1:X4}\r\n" +
                                 "DE: {2:X4}\r\n" +
                                 "HL: {3:X4}\r\n" +
                                 "PC: {4:X4}\r\n" +
                                 "SP: {5:X4}\r\n" +
                                 "IE: {6:X2} IF: {7:X2} IME: {8} \r\n",
                                 AF, BC, DE, HL, PC, SP, (byte) IE, (byte)IF, IME ? 1 : 0);
        }
    }
}
