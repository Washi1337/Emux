namespace Emux.GameBoy.Audio
{
    public class WaveSoundChannel : ISoundChannel
    {
        private readonly byte[] _waveRam = new byte[0x10];

        public byte NR0
        {
            get;
            set;
        }

        public byte NR1
        {
            get;
            set;
        }

        public byte NR2
        {
            get;
            set;
        }

        public byte NR3
        {
            get;
            set;
        }

        public byte NR4
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
            get { return (NR0 & (1 << 7)) != 0; }
        }

        public float SoundLength
        {
            get { return (256f - NR1) / 256f; }
        }

        public float OutputLevel
        {
            get
            {
                switch (NR2 >> 5 & 0b11)
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
                var value = NR3 | (NR4 & 0b111) << 8;
                return 4194304 / (64 * (2048 - value));
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
            if (Active)
            {
                   
            }
        }
    }
}
