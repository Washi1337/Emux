using System;
using System.Buffers;
using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Audio
{
    public class SquareChannel : ISoundChannel
    {
        private readonly VolumeEnvelope _volumeEnvelope;
        
        private int _coordinate = 0;
        private double _length = 0;

        private byte _nr4;
        private byte _nr2;
        private byte _nr1;
        private byte _nr0;
        private int _frequencyRegister;

        public SquareChannel(GameBoySpu spu)
        {
            Spu = spu ?? throw new ArgumentNullException(nameof(spu));
            ChannelVolume = 1;
            _volumeEnvelope = new VolumeEnvelope(this);
        }

        public GameBoySpu Spu
        {
            get;
        }

        public virtual int ChannelNumber
        {
            get { return 2; }
        }

        public virtual byte NR0
        {
            get { return _nr0; }
            set { _nr0 = value; }
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
            get { return (byte) (FrequencyRegister & 0xFF); }
            set { FrequencyRegister = (FrequencyRegister & ~0xFF) | value; }
        }

        public byte NR4
        {
            get { return _nr4; }
            set
            {
                _nr4 = (byte) (value & (1 << 6));
                if ((value & (1 << 7)) != 0)
                {
                    _length = SoundLength;
                    _coordinate = 0;
                }
                FrequencyRegister = (FrequencyRegister & 0xFF) | (value & 0b111) << 8;
            }
        }

        protected int FrequencyRegister
        {
            get { return _frequencyRegister; }
            set { _frequencyRegister = value & 0b111_1111_1111; }
        }

        protected float Frequency
        {
            get { return (float) (GameBoyCpu.OfficialClockFrequency / (32 * (2048 - FrequencyRegister))); }
            set { FrequencyRegister = (int) (2048 - GameBoyCpu.OfficialClockFrequency / (32 * value)); }
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

        public IAudioChannelOutput ChannelOutput
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the wave pattern duty, the fraction of one period in which the square signal is active.
        /// </summary>
        private float Duty
        {
            get
            {
                switch ((NR1 >> 6) & 0b11)
                {
                    case 0:
                        return 0.125f;
                    case 1:
                        return 0.25f;
                    case 2:
                        return 0.5f;
                    default:
                        return 0.75f;
                }
            }
        }

        /// <summary>
        /// Gets the length of the sound to be produced in seconds.
        /// </summary>
        public float SoundLength
        {
            get { return (64 - (NR1 & 0b111111)) * (1 / 256f); }
        }

        public bool UseSoundLength
        {
            get { return (NR4 & (1 << 6)) != 0; }
        }

        public virtual void ChannelStep(int cycles)
        {
            double cpuSpeedFactor = Spu.Device.SpeedFactor;
            if (!Active || double.IsNaN(cpuSpeedFactor) || double.IsInfinity(cpuSpeedFactor) || cpuSpeedFactor < 0.5)
                return;

            // Update volume and calculate wave amplitude.
            _volumeEnvelope.Update(cycles);
            double amplitude = ChannelVolume * (_volumeEnvelope.Volume / 15.0);

            // Obtain elapsed gameboy time. 
            double timeDelta = (cycles / GameBoyCpu.OfficialClockFrequency) / cpuSpeedFactor;
           
            // Allocate sound buffer.
            int sampleRate = ChannelOutput.SampleRate;
            int sampleCount = (int) Math.Ceiling(timeDelta * sampleRate) * 2;
            using (var malloc = MemoryPool<float>.Shared.Rent(sampleCount))
            {
                var buffer = malloc.Memory.Span;
                if (!UseSoundLength || _length >= 0)
                {
                    double period = 1f / Frequency;
                    for (int i = 0; i < sampleCount; i += 2)
                    {
                        // Get current x coordinate and compute current sample value. 
                        double x = (double)_coordinate / sampleRate;
                        float sample = DutyWave(amplitude, x, period);
                        Spu.WriteToSoundBuffer(ChannelNumber, buffer, i, sample);

                        _coordinate = (_coordinate + 1) % sampleRate;
                    }

                    if (UseSoundLength)
                        _length -= timeDelta;
                }

                ChannelOutput.BufferSoundSamples(buffer, 0, sampleCount);
            }
        }

        private float DutyWave(double amplitude, double x, double period)
        {
            // Pulse waves with a duty can be constructed by subtracting a saw wave from the same but shifted saw wave.
            float saw1 = (float) (-2 * amplitude / Math.PI * Math.Atan(Cot(x * Math.PI / period)));
            float saw2 = (float) (-2 * amplitude / Math.PI * Math.Atan(Cot(x * Math.PI / period - (1 - Duty) * Math.PI)));
            return saw1 - saw2;
        }

        private static double Cot(double x)
        {
            return 1 / Math.Tan(x);
        }
    }
}