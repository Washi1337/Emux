using System;
using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Audio
{
    public class NoiseChannel : ISoundChannel
    {
        private readonly Random _random = new Random();
        private readonly VolumeEnvelope _volumeEnvelope;

        private double _coordinate = 0;
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

        public void ChannelStep(int cycles)
        {
            double cpuSpeedFactor = Spu.Device.Cpu.SpeedFactor;
            if (!Active || double.IsNaN(cpuSpeedFactor) || double.IsInfinity(cpuSpeedFactor) || cpuSpeedFactor < 0.5)
                return;

            _volumeEnvelope.Update(cycles);

            double ratio = DividingRatio == 0 ? 0.5 : DividingRatio;
            double frequency = 524288 / ratio / Math.Pow(2, ShiftClockFrequency + 1);
            double period = (1 / frequency);

            int sampleRate = ChannelOutput.SampleRate;
            double timeDelta = (cycles / GameBoyCpu.OfficialClockFrequency) / cpuSpeedFactor;
            int sampleCount = (int) (timeDelta * sampleRate) * 2;
            float[] buffer = new float[sampleCount];

            if (!UseSoundLength || _length >= 0)
            {
                for (int i = 0; i < buffer.Length; i += 2)
                {
                    float sample = (float) (ChannelVolume * (_volumeEnvelope.Volume / 15.0) * _currentValue);

                    Spu.WriteToSoundBuffer(ChannelNumber, buffer, i, sample);

                    _coordinate += timeDelta;
                    if (_coordinate >= period)
                    {
                        _coordinate -= period;
                        _currentValue = _random.Next(0, 2) == 0 ? -1 : 1;
                    }
                }

                if (UseSoundLength)
                    _length -= timeDelta;
            }

            ChannelOutput.BufferSoundSamples(buffer, 0, buffer.Length);
        }
    }
}
