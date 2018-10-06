using System;
using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Audio
{
    public class VolumeEnvelope
    {
        private readonly ISoundChannel _channel;
        private double _timer;

        public VolumeEnvelope(ISoundChannel channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }
        
        public int Volume
        {
            get;
            private set;
        }

        public int InitialVolume
        {
            get { return _channel.NR2 >> 4; }
        }

        public bool EnvelopeIncrease
        {
            get { return (_channel.NR2 & (1 << 3)) != 0; }
        }

        public int EnvelopeSweepCount
        {
            get { return _channel.NR2 & 7; }
            set { _channel.NR2 = (byte) ((_channel.NR2 & ~7) | value & 7); }
        }

        public void Reset()
        {
            Volume = InitialVolume;
            _timer = 0;
        }
        
        public void Update(int cycles)
        {
            if (EnvelopeSweepCount > 0)
            {
                double timeDelta = (cycles / GameBoyCpu.OfficialClockFrequency) / _channel.Spu.Device.Cpu.SpeedFactor;
                _timer += timeDelta;

                double stepInterval = EnvelopeSweepCount / 64.0;
                while (_timer >= stepInterval)
                {
                    _timer -= stepInterval;
                    if (EnvelopeIncrease)
                        Volume++;
                    else
                        Volume--;
                    
                    if (Volume < 0)
                        Volume = 0;
                    if (Volume > 15)
                        Volume = 15;
                }

            }
        }
    }
}