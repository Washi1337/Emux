using System;
using System.Collections.Generic;

namespace Emux.GameBoy.Audio
{
    public class GameBoySpu : IGameBoyComponent
    {
        private readonly byte[] _unused = new byte[9];

        public GameBoySpu(GameBoy device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            Device = device;
            var channels = new List<ISoundChannel>();
            channels.Add(new SquareSweepChannel(this));
            channels.Add(new SquareChannel(this));
            channels.Add(Wave = new WaveSoundChannel(this));
            channels.Add(new NoiseChannel(this));
            Channels = channels.AsReadOnly();
            ActivateAllChannels();
        }

        public GameBoy Device
        {
            get;
        }

        public byte NR50
        {
            get;
            set;
        }

        public SpuOutputSelection NR51
        {
            get;
            set;
        }

        public byte NR52
        {
            get;
            set;
        }

        public bool EnableSO1
        {
            get { return (NR50 & (1 << 3)) != 0; }
            set { NR50 = (byte)((NR50 & ~(1 << 3)) | (value ? 1 : 0) << 3); }
        }

        public byte SO1Volume
        {
            get { return (byte)(NR50 & 0x7); }
            set { NR50 = (byte)((NR50 & ~0x7) | (value << 4)); }
        }

        public bool EnableSO2
        {
            get { return (NR50 & (1 << 7)) != 0; }
            set { NR50 = (byte) ((NR50 & ~(1 << 7)) | (value ? 1 : 0) << 7); }
        }

        public byte SO2Volume
        {
            get { return (byte) ((NR50 >> 4) & 0x7); }
            set { NR50 = (byte) ((NR50 & ~(0x7 << 4)) | ((value & 0x7) << 4)); }
        }

        public IList<ISoundChannel> Channels
        {
            get;
        }

        public WaveSoundChannel Wave
        {
            get;
        }

        public void Initialize()
        {
            Reset();
        }

        public void Reset()
        {
            Channels[0].NR0 = 0x80;
            Channels[0].NR1 = 0xBF;
            Channels[0].NR2 = 0xF3;
            Channels[0].NR4 = 0xBF;
            
            Channels[1].NR1 = 0x3F;
            Channels[1].NR2 = 0x00;
            Channels[1].NR4 = 0xBF;
            
            Channels[2].NR0 = 0x7F;
            Channels[2].NR1 = 0xFF;
            Channels[2].NR2 = 0x9F;
            Channels[2].NR3 = 0xBF;
            
            Channels[3].NR1 = 0xFF;
            Channels[3].NR2 = 0x00;
            Channels[3].NR3 = 0x00;
            Channels[3].NR4 = 0xBF;

            NR50 = 0x77;
            NR51 = (SpuOutputSelection) 0xF3;
            NR52 = 0xF1;
        }

        public void Shutdown()
        {
        }

        public void WriteRegister(ushort address, byte value)
        {
            switch (address)
            {
                case 0xFF24:
                    NR50 = value;
                    break;
                case 0xFF25:
                    NR51 = (SpuOutputSelection) value;
                    break;
                case 0xFF26:
                    NR52 = value;
                    break;
                default:
                    if (address >= 0xFF27 && address < 0xFF30)
                    {
                        _unused[address - 0xFF27] = value;
                    }
                    else if (address >= 0xFF30 && address < 0xFF40)
                    {
                        Wave.WriteWavRam((ushort) (address - 0xFF30), value);
                    }
                    else
                    {
                        int relativeAddress = address - 0xFF10;
                        int channelIndex = relativeAddress / 5;
                        var channel = Channels[channelIndex];
                        
                        switch (relativeAddress % 5)
                        {
                            case 0:
                                channel.NR0 = value;
                                break;
                            case 1:
                                channel.NR1 = value;
                                break;
                            case 2:
                                channel.NR2 = value;
                                break;
                            case 3:
                                channel.NR3 = value;
                                break;
                            case 4:
                                channel.NR4 = value;
                                break;
                        }
                    }
                    break;
            }
        }

        public byte ReadRegister(ushort address)
        {
            switch (address)
            {
                case 0xFF24:
                    return NR50;
                case 0xFF25:
                    return (byte) NR51;
                case 0xFF26:
                    return NR52;
                default:
                    if (address >= 0xFF27 && address < 0xFF30)
                        return _unused[address - 0xFF27];
                    if (address >= 0xFF30 && address < 0xFF40)
                        return Wave.ReadWavRam((ushort) (address - 0xFF30));

                    int relativeAddress = address - 0xFF10;
                    int channelIndex = relativeAddress / 5;
                    var channel = Channels[channelIndex];

                    // TODO: remove
                    if (channel == null)
                        return 0;

                    switch (relativeAddress % 5)
                    {
                        case 0:
                            return channel.NR0;
                        case 1:
                            return channel.NR1;
                        case 2:
                            return channel.NR2;
                        case 3:
                            return channel.NR3;
                        case 4:
                            return channel.NR4;
                    }
                    throw new ArgumentOutOfRangeException(nameof(address));
            }
        }

        public void SpuStep(int cycles)
        {
            if ((NR52 & (1 << 7)) != 0)
            {
                foreach (var channel in Channels)
                    channel?.ChannelStep(cycles);
            }
        }

        public void ActivateAllChannels()
        {
            foreach (var channel in Channels)
            {
                if (channel != null)
                    channel.Active = true;
            }
        }

        public void DeactivateAllChannels()
        {
            foreach (var channel in Channels)
            {
                if (channel != null)
                    channel.Active = false;
            }
        }

        internal void WriteToSoundBuffer(int channel, float[] totalBuffer, int index, float sample)
        {
            sample *= SO1Volume / 7f;
            if (((int)NR51 & (1 << (channel - 1))) != 0)
                totalBuffer[index + 1] = sample;
            if (((int)NR51 & (1 << (channel + 3))) != 0)
                totalBuffer[index] = sample;
        }
    }
}
