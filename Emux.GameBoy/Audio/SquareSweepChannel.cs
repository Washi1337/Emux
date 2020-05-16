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

        public override int ChannelNumber
        {
            get { return 1; }
        }

        public override byte NR0
        {
            get { return base.NR0; }
            set
            {
                base.NR0 = value;
                _frequencySweepClock = 0;
            }
        }

        public float SweepTime
        {
            get { return ((NR0 >> 4) & 7) / 128f; }
        }

        public bool SweepIncrease
        {
            get { return ((NR0 >> 3) & 1) == 0; }
        }

        public int SweepShiftCount
        {
            get { return NR0 & 0b111; }
            set { NR0 = (byte) ((NR0 & ~0b111) | value & 0b111); }
        }

        protected void UpdateFrequency(int cycles)
        {
            if (SweepTime > 0 && _spu.Device.SpeedFactor > 0.5)
            {
                double timeDelta = (cycles / GameBoyCpu.OfficialClockFrequency) / _spu.Device.SpeedFactor;
                _frequencySweepClock += timeDelta;

                while (_frequencySweepClock >= SweepTime)
                {
                    _frequencySweepClock -= SweepTime;
                    
                    int delta = (int) (FrequencyRegister / Math.Pow(2, SweepShiftCount));
                    if (!SweepIncrease)
                        delta = -delta;
                    FrequencyRegister = FrequencyRegister + delta;
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
