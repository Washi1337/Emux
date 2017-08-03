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

        protected int Frequency = 0;
        private byte _nr4;
        private byte _nr2;
        private byte _nr1;
        private byte _nr0;

        public SquareChannel(GameBoySpu spu)
        {
            if (spu == null)
                throw new ArgumentNullException(nameof(spu));
            _spu = spu;
        }

        public byte NR0
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
            get { return (byte) (Frequency & 0xFF); }
            set { Frequency = (Frequency & ~0xFF) | value; }
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
                Frequency = (Frequency & 0xFF) | (value & 0b111) << 8;
            }
        }
        public bool Active
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
                double timeDelta = (cycles / GameBoyCpu.OfficialClockFrequency) / _spu.Device.Cpu.SpeedFactor * 2;
                _volumeEnvelopeTimer += timeDelta;

                double stepInterval = EnvelopeSweepCount / 64.0 * 2;
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
            if (!Active || _spu.Device.Cpu.SpeedFactor < 0.5)
                return;

            UpdateVolume(cycles);

            const float maxAmplitude = 0.05f;

            double realFrequency = 131072.0 / (2048.0 - Frequency);
            int sampleRate = ChannelOutput.SampleRate;
            double timeDelta = (cycles / GameBoyCpu.OfficialClockFrequency) / _spu.Device.Cpu.SpeedFactor * 2;
            int sampleCount = (int) (timeDelta * sampleRate);
            float[] buffer = new float[sampleCount];

            if (!UseSoundLength || _length >= 0)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (float) (maxAmplitude * (_volume / 15.0)                                // Volume adjustments.
                        * Math.Sign(Math.Sin(2 * Math.PI * realFrequency * _coordinate / sampleRate))); // Square wave formula
                    _coordinate = (_coordinate + 1) % sampleRate;
                }

                if(UseSoundLength)
                    _length -= timeDelta;
            }

            ChannelOutput.BufferSoundSamples(buffer, 0, buffer.Length);
        }

    }
}