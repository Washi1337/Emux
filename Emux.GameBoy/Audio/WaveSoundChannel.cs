using System;
using System.Buffers;
using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Audio
{
    public class WaveSoundChannel : ISoundChannel
    {
        private readonly byte[] _waveRam = new byte[0x10];
        private byte _nr0;
        private byte _nr1;
        private byte _nr2;
        private byte _nr3;
        private byte _nr4;
        private int _coordinate;
        private bool _top;

        public WaveSoundChannel(GameBoySpu spu)
        {
            Spu = spu ?? throw new ArgumentNullException(nameof(spu));
            ChannelVolume = 1;
        }

        public GameBoySpu Spu
        {
            get;
        }

        public virtual int ChannelNumber
        {
            get { return 3; }
        }

        public byte NR0
        {
            get { return _nr0; }
            set { _nr0 = value; }
        }

        public byte NR1
        {
            get { return _nr1; }
            set { _nr1 = value; }
        }

        public byte NR2
        {
            get { return _nr2; }
            set { _nr2 = value; }
        }

        public byte NR3
        {
            get { return _nr3; }
            set { _nr3 = value; }
        }

        public byte NR4
        {
            get { return _nr4; }
            set { _nr4 = value; }
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

        public bool Active
        {
            get;
            set;
        }

        public bool SoundEnabled
        {
            get { return (_nr0 & (1 << 7)) != 0; }
        }

        public float SoundLength
        {
            get { return (256f - _nr1) / 256f; }
        }

        public float OutputLevel
        {
            get
            {
                switch (_nr2 >> 5 & 0b11)
                {
                    default:
                        return 0f;
                    case 1:
                        return 1f;
                    case 2:
                        return 0.5f;
                    case 3:
                        return 0.25f;
                }
            }
        }

        public int Frequency
        {
            get
            {
                var value = _nr3 | (_nr4 & 0b111) << 8;
                return (int) (GameBoyCpu.OfficialClockFrequency / (64 * (2048 - value)));
            }
        }

        public byte ReadWavRam(ushort address)
        {
            return _waveRam[address];
        }

        public void WriteWavRam(ushort address, byte value)
        {
            _waveRam[address] = value;
        }

        public void ChannelStep(int cycles)
        {
            double cpuSpeedFactor = Spu.Device.SpeedFactor;
            if (!Active)
                return;

            int sampleRate = ChannelOutput.SampleRate;
            double timeDelta = (cycles / GameBoyCpu.OfficialClockFrequency) / cpuSpeedFactor;
            int sampleCount = (int) (timeDelta * sampleRate) * 2;
            using (var malloc = MemoryPool<float>.Shared.Rent(sampleCount))
            {
                var buffer = malloc.Memory.Span;

                double interval = 1.0 / Frequency;
                int intervalSampleCount = (int)(interval * sampleRate);

                if (intervalSampleCount > 0)
                {
                    for (int i = 0; i < sampleCount; i += 2)
                    {
                        _coordinate++;
                        if (_coordinate >= intervalSampleCount)
                        {
                            _top = !_top;
                            _coordinate = 0;
                        }

                        int waveRamCoordinate = (int)(_coordinate / (double)intervalSampleCount * _waveRam.Length);

                        int waveDataSample = _top
                            ? (_waveRam[waveRamCoordinate] & 0xF)
                            : ((_waveRam[waveRamCoordinate] >> 4) & 0xF);

                        float sample = ChannelVolume * OutputLevel * (waveDataSample - 7) / 15f;

                        Spu.WriteToSoundBuffer(ChannelNumber, buffer, i, sample);
                    }
                }

                ChannelOutput.BufferSoundSamples(buffer, 0, sampleCount);
            }
        }
    }
}
