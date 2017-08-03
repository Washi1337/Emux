using System;
using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Audio
{
    public class SquareSweepChannel : SquareChannel
    {
        private readonly GameBoySpu _spu;
        private double _frequencySweepClock;

        public SquareSweepChannel(GameBoySpu spu)
            : base(spu)
        {
            _spu = spu;
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

        protected void UpdateFrequency(int cycles)
        {
            if (SweepShiftCount > 0)
            {
                double timeDelta = (cycles / GameBoyCpu.OfficialClockFrequency) / _spu.Device.Cpu.SpeedFactor * 2;
                _frequencySweepClock += timeDelta;
                
                double sweepInterval = 1.0 / SweepFrequency;
                while (_frequencySweepClock >= sweepInterval)
                {
                    _frequencySweepClock -= sweepInterval;
                    int delta = (int) (Frequency / Math.Pow(2, SweepShiftCount));
                    if (!SweepIncrease)
                        delta = -delta;
                    Frequency = Frequency + delta;
                }
            }
        }

        public override void ChannelStep(int cycles)
        {
            UpdateFrequency(cycles);
            base.ChannelStep(cycles);
        }
    }
}
