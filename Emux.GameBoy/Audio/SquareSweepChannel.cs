using System;

namespace Emux.GameBoy.Audio
{
    public class SquareSweepChannel : SquareChannel
    {
        public SquareSweepChannel(GameBoySpu spu)
            : base(spu)
        {
        }

        public float SweepFrequency
        {
            get { return (NR0 >> 4) / 128f; }
        }

        public bool SweepIncrease
        {
            get { return ((NR0 >> 3) & 1) == 0; }
        }

        public int SweepShiftCount
        {
            get { return NR0 & 0b111; }
        }

        protected void ComputeNextFrequency()
        {
            int delta = (int) (Frequency / Math.Pow(2, SweepShiftCount));
            if (!SweepIncrease)
                delta = -delta;
            Frequency = Frequency + delta;
        }
    }
}
