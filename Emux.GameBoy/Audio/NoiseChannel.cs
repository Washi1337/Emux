using System;
using System.Buffers;
using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Audio
{
    public class NoiseChannel : ISoundChannel
    {
        private readonly Random _random = new Random();
        private readonly VolumeEnvelope _volumeEnvelope;
        private readonly LfsRegister _lfsr;

        private int _clock = 0;
        private double _length = 0;
        private double _currentValue = 0;

        private byte _nr1;
        private byte _nr2;
        private byte _nr3;
        private byte _nr4;

        public NoiseChannel(GameBoySpu spu)
        {
            Spu = spu ?? throw new ArgumentNullException(nameof(spu));
            ChannelVolume = 1;
            _volumeEnvelope = new VolumeEnvelope(this);
            _lfsr = new LfsRegister(this);
        }

        public GameBoySpu Spu
        {
            get;
        }

        public virtual int ChannelNumber
        {
            get { return 4; }
        }

        public byte NR0
        {
            get { return 0; }
            set { }
        }

        public byte NR1
        {
            get { return _nr1; }
            set
            {
                _nr1 = value;
                _length = SoundLength;
            }
        }

        public byte NR2
        {
            get { return _nr2; }
            set
            {
                _nr2 = value;
                _volumeEnvelope.Reset();
            }
        }

        public byte NR3
        {
            get { return _nr3; }
            set
            {
                _nr3 = value;
                _clock = 0;
                _lfsr.Reset();
            }
        }

        public byte NR4
        {
            get { return _nr4; }
            set { _nr4 = value; }
        }

        public bool Active
        {
            get;
            set;
        }

        public float ChannelVolume
        {
            get;
            set;
        }

        public double SoundLength
        {
            get { return (64 - (_nr1 & 63)) / 256.0; }
        }
        
        public int ShiftClockFrequency
        {
            get { return NR3 >> 4; }
            set { NR3 = (byte) ((NR3 & 0b1111) | (value << 4)); }
        }

        public int DividingRatio
        {
            get { return NR3 & 0b111; }
            set { NR3 = (byte) ((NR3 & ~0b111) | value & 0b111); }
        }

        public float Frequency
        {
            get
            {
                double ratio = DividingRatio == 0 ? 0.5 : DividingRatio;
                return (float) (GameBoyCpu.OfficialClockFrequency / 8 / ratio / Math.Pow(2, ShiftClockFrequency + 1));
            }
        }
        
        public bool UseSoundLength
        {
            get { return (_nr4 & (1 << 6)) != 0; }
        }

        public IAudioChannelOutput ChannelOutput
        {
            get;
            set;
        }

        public void ChannelStep(int cycles)
        {
            double cpuSpeedFactor = Spu.Device.SpeedFactor;
            if (!Active)
                return;

            // Update volume.
            _volumeEnvelope.Update(cycles);
            float amplitude = ChannelVolume * _volumeEnvelope.Volume / 15.0f;
            
            // Get elapsed gameboy time.
            double timeDelta = (cycles / GameBoyCpu.OfficialClockFrequency) / cpuSpeedFactor;
            
            // Allocate buffer.
            int sampleRate = ChannelOutput.SampleRate;
            int sampleCount = (int) (timeDelta * sampleRate) * 2;
            using (var malloc = MemoryPool<float>.Shared.Rent(sampleCount))
            {
                var buffer = malloc.Memory.Span;

                if (!UseSoundLength || _length >= 0)
                {
                    double period = 1 / Frequency;
                    int periodSampleCount = (int)(period * sampleRate) * 2;

                    for (int i = 0; i < sampleCount; i += 2)
                    {
                        float sample = amplitude * (_lfsr.CurrentValue ? 1f : 0f);
                        Spu.WriteToSoundBuffer(ChannelNumber, buffer, i, sample);

                        _clock += 2;
                        if (_clock >= periodSampleCount)
                        {
                            _lfsr.PerformShift();
                            _clock -= periodSampleCount;
                        }
                    }

                    if (UseSoundLength)
                        _length -= timeDelta;
                }

                ChannelOutput.BufferSoundSamples(buffer, 0, sampleCount);
            }
        }
    }
}
