using System;

namespace Emux.GameBoy.Cpu
{
    public class GameBoyAlu
    {
        private readonly RegisterBank _registers;

        public GameBoyAlu(RegisterBank registers)
        {
            _registers = registers;
        }

        private ushort Perform(ushort a, ushort b, Func<ushort, ushort, int> operation, RegisterFlags affectedFlags)
        {
            int intermediate = operation(a, b);
            ushort result = (ushort) (intermediate & 0xFFFF);
            _registers.ClearFlags(affectedFlags);
            var setAffected = RegisterFlags.None;
            if ((intermediate & (1 << 16)) == (1 << 16))
                setAffected |= RegisterFlags.C;
            if ((operation((ushort) (a & 0xFFF), (ushort) (b & 0xFFF)) & (1 << 12)) == (1 << 12))
                setAffected |= RegisterFlags.H;
            if (result == 0)
                setAffected |= RegisterFlags.Z;
            _registers.SetFlags(setAffected & affectedFlags);
            return result;
        }

        private byte Perform(byte a, byte b, Func<byte, byte, int> operation, RegisterFlags affectedFlags)
        {
            int intermediate = operation(a, b);
            byte result = (byte) (intermediate & 0xFF);
            _registers.ClearFlags(affectedFlags);
            var setAffected = RegisterFlags.None;
            if ((intermediate & (1 << 8)) == (1 << 8))
                setAffected |= RegisterFlags.C;
            if ((operation((byte) (a & 0xF), (byte) (b & 0xF)) & (1 << 4)) == (1 << 4))
                setAffected |= RegisterFlags.H;
            if (result == 0)
                setAffected |= RegisterFlags.Z;
            _registers.SetFlags(setAffected & affectedFlags);
            return result;
        }

        private ushort Perform(ushort a, sbyte b, Func<ushort, sbyte, int> operation, RegisterFlags affectedFlags)
        {
            int intermediate = operation(a, b);
            ushort result = (ushort) (intermediate & 0xFFFF);
            _registers.ClearFlags(affectedFlags);
            var setAffected = RegisterFlags.None;
            if ((intermediate & (1 << 8)) == (1 << 8))
                setAffected |= RegisterFlags.C;
            if ((operation((ushort) (a & 0xF), (sbyte) (b & 0xF)) & (1 << 4)) == (1 << 4))
                setAffected |= RegisterFlags.H;
            if (result == 0)
                setAffected |= RegisterFlags.Z;
            _registers.SetFlags(setAffected & affectedFlags);
            return result;
        }

        internal ushort Add(ushort a, ushort b, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            _registers.ClearFlags(resetFlags);
            _registers.SetFlags(setFlags);
            return Perform(a, b, (x, y) => x + y, affectedFlags);
        }

        internal ushort Add(ushort a, sbyte b, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            _registers.ClearFlags(resetFlags);
            _registers.SetFlags(setFlags);
            return Perform(a, b, (x, y) => x + y, affectedFlags);
        }

        internal byte Add(byte a, byte b, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            _registers.ClearFlags(resetFlags);
            _registers.SetFlags(setFlags);
            return Perform(a, b, (x, y) => x + y, affectedFlags);
        }

        internal ushort Sub(ushort a, ushort b, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            _registers.ClearFlags(resetFlags);
            _registers.SetFlags(setFlags);
            return Perform(a, b, (x, y) => x - y, affectedFlags);
        }

        internal byte Sub(byte a, byte b, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            _registers.ClearFlags(resetFlags);
            _registers.SetFlags(setFlags);
            return Perform(a, b, (x, y) => x - y, affectedFlags);
        }

        internal byte Adc(byte a, byte b, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            _registers.ClearFlags(resetFlags);
            _registers.SetFlags(setFlags);
            return Perform(a, b, (x, y) => x + y + (_registers.GetFlags(RegisterFlags.C) ? 1 : 0), affectedFlags);
        }

        internal byte Sbc(byte a, byte b, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            _registers.ClearFlags(resetFlags);
            _registers.SetFlags(setFlags);
            return Perform(a, b, (x, y) => x - (y + (_registers.GetFlags(RegisterFlags.C) ? 1 : 0)), affectedFlags);
        }

        internal byte And(byte b)
        {
            unchecked
            {
                int intermediate = _registers.A & b;
                byte result = (byte) (intermediate & 0xFF);
                _registers.OverwriteFlags(RegisterFlags.H | (result == 0 ? RegisterFlags.Z : RegisterFlags.None));
                return result;
            }
        }

        internal byte Xor(byte b)
        {
            unchecked
            {
                int intermediate = _registers.A ^ b;
                byte result = (byte) (intermediate & 0xFF);
                _registers.OverwriteFlags(result == 0 ? RegisterFlags.Z : RegisterFlags.None);
                return result;
            }
        }

        internal byte Or(byte b)
        {
            unchecked
            {
                int intermediate = _registers.A | b;
                byte result = (byte) (intermediate & 0xFF);
                _registers.OverwriteFlags(result == 0 ? RegisterFlags.Z : RegisterFlags.None);
                return result;
            }
        }

        internal void Cp(byte b)
        {
            Sub(_registers.A, b, RegisterFlags.Z | RegisterFlags.H | RegisterFlags.C, RegisterFlags.N);
        }

        internal byte Rl(byte value, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            byte newValue = (byte) ((value << 1) | (_registers.GetFlags(RegisterFlags.C) ? 1 : 0));

            _registers.ClearFlags(affectedFlags | resetFlags);
            _registers.SetFlags(setFlags);

            var flags = RegisterFlags.None;
            if (newValue == 0)
                flags |= RegisterFlags.Z;
            if ((value & (1 << 7)) == (1 << 7))
                flags |= RegisterFlags.C;
            _registers.SetFlags(flags);

            return newValue;
        }

        internal byte Rlc(byte value, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            byte newValue = (byte) ((value << 1) | (value >> 7));

            _registers.ClearFlags(affectedFlags | resetFlags);
            _registers.SetFlags(setFlags);
            var flags = RegisterFlags.None;
            if (newValue == 0)
                flags |= RegisterFlags.Z;
            if ((value & (1 << 7)) == (1 << 7))
                flags |= RegisterFlags.C;
            _registers.SetFlags(flags);

            return newValue;
        }

        internal byte Rr(byte value, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            byte newValue = (byte) ((value >> 1) | (_registers.GetFlags(RegisterFlags.C) ? 1 : 0));

            _registers.ClearFlags(affectedFlags | resetFlags);
            _registers.SetFlags(setFlags);
            var flags = RegisterFlags.None;
            if (newValue == 0)
                flags |= RegisterFlags.Z;
            if ((value & 1) == 1)
                flags |= RegisterFlags.C;
            _registers.SetFlags(flags);

            return newValue;
        }

        internal byte Rrc(byte value, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            byte newValue = (byte) ((value >> 1) | ((value & 1) << 7));

            _registers.ClearFlags(affectedFlags | resetFlags);
            _registers.SetFlags(setFlags);
            var flags = RegisterFlags.None;
            if (newValue == 0)
                flags |= RegisterFlags.Z;
            if ((value & 1) == 1)
                flags |= RegisterFlags.C;
            _registers.SetFlags(flags);

            return newValue;
        }

        internal byte Sla(byte value)
        {
            byte newValue = (byte) (value << 1);
            var flags = RegisterFlags.None;
            if (newValue == 0)
                flags |= RegisterFlags.Z;
            if ((value & (1 << 7)) == (1 << 7))
                flags |= RegisterFlags.C;
            _registers.OverwriteFlags(flags);
            return newValue;
        }

        internal byte Sr(byte value, bool resetMsb)
        {
            byte newValue = (byte) (value >> 1);
            if (!resetMsb)
                newValue |= (byte) (value & (1 << 7));
            var flags = RegisterFlags.None;
            if (newValue == 0)
                flags |= RegisterFlags.Z;
            if ((value & 1) == 1)
                flags |= RegisterFlags.C;
            _registers.OverwriteFlags(flags);
            return newValue;
        }

        internal byte Swap(byte value)
        {
            byte newValue = (byte) (((value & 0xF) << 4) | ((value & 0xF0) >> 4));
            _registers.OverwriteFlags(newValue == 0 ? RegisterFlags.Z : RegisterFlags.None);
            return newValue;
        }

        internal byte Set(byte value, int position)
        {
            return (byte) (value | (1 << position));
        }

        internal byte Res(byte value, int position)
        {
            return (byte) (value & ~(1 << position));
        }

        internal void Bit(byte value, int position)
        {
            var c = _registers.GetFlags(RegisterFlags.C) ? RegisterFlags.C : RegisterFlags.None;
            var z = ((value >> position) & 1) == 0 ? RegisterFlags.Z : RegisterFlags.None;
            _registers.OverwriteFlags(z | RegisterFlags.H | c);
        }

        internal ushort Increment(ushort value, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            return Add(value, 1, affectedFlags, setFlags, resetFlags);
        }

        internal ushort Decrement(ushort value, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            return Sub(value, 1, affectedFlags, setFlags, resetFlags);
        }

        internal byte Increment(byte value, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            return Add(value, (byte) 1, affectedFlags, setFlags, resetFlags);
        }

        internal byte Decrement(byte value, RegisterFlags affectedFlags = RegisterFlags.None,
            RegisterFlags setFlags = RegisterFlags.None, RegisterFlags resetFlags = RegisterFlags.None)
        {
            return Sub(value, 1, affectedFlags, setFlags, resetFlags);
        }

        internal byte Cpl(byte value)
        {
            unchecked
            {
                _registers.SetFlags(RegisterFlags.N | RegisterFlags.H);
                return (byte) ~value;
            }
        }

        internal void Ccf()
        {
            if (_registers.GetFlags(RegisterFlags.C))
                _registers.ClearFlags(RegisterFlags.C);
            else
                _registers.SetFlags(RegisterFlags.C);
        }

        internal void Daa()
        {
            var flags = RegisterFlags.None;

            ushort value = _registers.A;
            if ((value & 0xF) > 0x9 || _registers.GetFlags(RegisterFlags.H))
                value += 6;

            if ((value & 0xF0) > 0x90)
                value += 0x60;
            
            if ((value & (1 << 8)) == (1 << 8))
                flags |= RegisterFlags.C;
            if (_registers.A == 0)
                flags |= RegisterFlags.Z;

            _registers.A = (byte) (value & 0xFF);

            _registers.ClearFlags(RegisterFlags.Z | RegisterFlags.H | RegisterFlags.C);
            _registers.SetFlags(flags);
        }
    }
}
