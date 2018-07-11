using System;
using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Audio
{
    public class SquareChannel : ISoundChannel
    {
        private readonly GameBoySpu _spu;
        private int _coordinate = 0;
        private double _length = 0;
        private int _volume = 0;
        private double _volumeEnvelopeTimer = 0;

        private byte _nr4;
        private byte _nr2;
        private byte _nr1;
        private byte _nr0;
        private int _frequencyRegister;

        public SquareChannel(GameBoySpu spu)
        {
            if (spu == null)
                throw new ArgumentNullException(nameof(spu));
            _spu = spu;
            ChannelVolume = 1;
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
                _volume = InitialEnvelopeVolume;
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

        public int InitialEnvelopeVolume
        {
            get { return NR2 >> 4; }
        }

        public bool EnvelopeIncrease
        {
            get { return (NR2 & (1 << 3)) != 0; }
        }

        public int EnvelopeSweepCount
        {
            get { return NR2 & 7; }
            set { NR2 = (byte) ((NR2 & ~7) | value & 7); }
        }

        private void UpdateVolume(int cycles)
        {
            if (EnvelopeSweepCount > 0)
            {
                double timeDelta = (cycles / GameBoyCpu.OfficialClockFrequency) / _spu.Device.Cpu.SpeedFactor;
                _volumeEnvelopeTimer += timeDelta;

                double stepInterval = EnvelopeSweepCount / 64.0;
                while (_volumeEnvelopeTimer >= stepInterval)
                {
                    _volumeEnvelopeTimer -= stepInterval;
                    if (EnvelopeIncrease)
                        _volume++;
                    else
                        _volume--;
                    
                    if (_volume < 0)
                        _volume = 0;
                    if (_volume > 15)
                        _volume = 15;
                }

            }
        }

        public virtual void ChannelStep(int cycles)
        {
            double cpuSpeedFactor = _spu.Device.Cpu.SpeedFactor;
            if (!Active || double.IsNaN(cpuSpeedFactor) || double.IsInfinity(cpuSpeedFactor) || cpuSpeedFactor < 0.5)
                return;

            UpdateVolume(cycles);

            double realFrequency = Frequency;
            int sampleRate = ChannelOutput.SampleRate;
            double timeDelta = (cycles / GameBoyCpu.OfficialClockFrequency) / cpuSpeedFactor;
           
            int sampleCount = (int) (timeDelta * sampleRate) * 2;
            var buffer = new float[sampleCount];

            double amplitude = ChannelVolume * (_volume / 15.0);
            double period = (float) (1f / realFrequency);
            
            if (!UseSoundLength || _length >= 0)
            {
                for (int i = 0; i < buffer.Length; i += 2)
                {
                    double x = (double) _coordinate / sampleRate;
                    float saw1 = (float) (-2 * amplitude / Math.PI * Math.Atan(Cot(x * Math.PI / period)));
                    float saw2 = (float) (-2 * amplitude / Math.PI * Math.Atan(Cot(x * Math.PI / period - (1-Duty)*Math.PI)));

                    float sample = saw1 - saw2;

                    _spu.WriteToSoundBuffer(ChannelNumber, buffer, i, sample);

                    _coordinate = (_coordinate + 1) % sampleRate;
                }

                if (UseSoundLength)
                    _length -= timeDelta;
            }

            ChannelOutput.BufferSoundSamples(buffer, 0, buffer.Length);
        }

        internal static double Cot(double x)
        {
            return 1 / Math.Tan(x);
        }

    }
}