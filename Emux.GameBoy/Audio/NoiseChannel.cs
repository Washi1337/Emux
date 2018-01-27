using System;
using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Audio
{
    public class NoiseChannel : ISoundChannel
    {
        private readonly GameBoySpu _spu;
        private readonly Random _random = new Random();

        private double _coordinate = 0;
        private double _length = 0;
        private double _currentValue = 0;
        private int _volume = 0;
        private double _volumeEnvelopeTimer = 0;
        private byte _nr1;
        private byte _nr2;
        private byte _nr3;
        private byte _nr4;

        public NoiseChannel(GameBoySpu spu)
        {
            if (spu == null)
                throw new ArgumentNullException(nameof(spu));
            _spu = spu;
            ChannelVolume = 1;
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
                _volume = InitialEnvelopeVolume;
            }
        }

        public byte NR3
        {
            get { return _nr3; }
            set
            {
                _nr3 = value;
                _coordinate = 0;
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

        public int InitialEnvelopeVolume
        {
            get { return _nr2 >> 4; }
        }

        public bool EnvelopeIncrease
        {
            get { return (_nr2 & (1 << 3)) != 0; }
        }

        public int EnvelopeSweepCount
        {
            get { return _nr2 & 7; }
            set { _nr2 = (byte)((_nr2 & ~7) | (value & 7)); }
        }

        public int ShiftClockFrequency
        {
            get { return _nr3 >> 4; }
            set { _nr3 = (byte) ((_nr3 & 0b1111) | (value << 4)); }
        }

        public bool Use7BitStepWidth
        {
            get { return (_nr3 & (1 << 3)) != 0; }
            set { _nr3 = (byte) ((_nr3 & ~(1 << 3)) | (value ? (1 << 3) : 0)); }
        }

        public int DividingRatio
        {
            get { return _nr3 & 0b111; }
            set { _nr3 = (byte) ((_nr3 & ~0b111) | value & 0b111); }
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
        
        public void ChannelStep(int cycles)
        {
            double cpuSpeedFactor = _spu.Device.Cpu.SpeedFactor;
            if (!Active || double.IsNaN(cpuSpeedFactor) || double.IsInfinity(cpuSpeedFactor) || cpuSpeedFactor < 0.5)
                return;

            UpdateVolume(cycles);

            double ratio = DividingRatio == 0 ? 0.5 : DividingRatio;
            double frequency = 524288 / ratio / Math.Pow(2, ShiftClockFrequency + 1) * 2;

            int sampleRate = ChannelOutput.SampleRate;
            double timeDelta = (cycles / GameBoyCpu.OfficialClockFrequency) / cpuSpeedFactor;
            int sampleCount = (int) (timeDelta * sampleRate) * 2;
            float[] buffer = new float[sampleCount];

            if (!UseSoundLength || _length >= 0)
            {
                for (int i = 0; i < buffer.Length; i += 2)
                {
                    float sample = (float) (ChannelVolume * (_volume / 15.0) * _currentValue);

                    _spu.WriteToSoundBuffer(ChannelNumber, buffer, i, sample);

                    _coordinate += timeDelta;
                    if (_coordinate >= (1 / frequency) * 2)
                    {
                        _coordinate -= (1 / frequency) * 2;
                        _currentValue = _random.NextDouble();
                    }
                }

                if (UseSoundLength)
                    _length -= timeDelta;
            }

            ChannelOutput.BufferSoundSamples(buffer, 0, buffer.Length);
        }
    }
}
