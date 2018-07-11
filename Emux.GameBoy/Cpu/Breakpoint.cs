using System;

namespace Emux.GameBoy.Cpu
{
    public class Breakpoint
    {
        public static readonly Predicate<GameBoyCpu> BreakAlways = _ => true;

        public Breakpoint(ushort offset)
            : this(offset, BreakAlways)
        {
        }

        public Breakpoint(ushort offset, Predicate<GameBoyCpu> condition)
        {
            Offset = offset;
            Condition = condition;
        }
        
        public ushort Offset
        {
            get;
        }

        public Predicate<GameBoyCpu> Condition
        {
            get;
            set;
        }

        public override string ToString()
        {
            return Offset.ToString("X4");
        }
    }
}