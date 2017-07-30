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

        public bool Enabled
        {
            get { return (NR0 & (1 << 7)) != 0; }
        }

        public byte ReadWavRam(ushort address)
        {
            return _waveRam[address];
        }

        public void WriteWavRam(ushort address, byte value)
        {
            _waveRam[address] = value;
        }
    }
}
