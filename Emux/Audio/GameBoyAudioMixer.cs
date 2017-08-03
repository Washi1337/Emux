using System.Collections;
using System.Collections.Generic;
using Emux.GameBoy.Audio;
using NAudio.Wave;

namespace Emux.Audio
{
    public class GameBoyAudioMixer : IWaveProvider
    {
        private readonly MixingWaveProvider32 _mixer = new MixingWaveProvider32();

        public GameBoyAudioMixer()
        {
            Channels = new List<NAudioChannelOutput>
            {
                new NAudioChannelOutput(this),
                new NAudioChannelOutput(this),
                new NAudioChannelOutput(this),
                new NAudioChannelOutput(this),
            }.AsReadOnly();

            foreach (var channel in Channels)
                _mixer.AddInputStream(channel);
        }
        
        public WaveFormat WaveFormat
        {
            get { return _mixer.WaveFormat; }
        }

        public IList<NAudioChannelOutput> Channels
        {
            get;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            _mixer.Read(buffer, offset, count);
            return count;
        }

        public void Connect(GameBoySpu spu)
        {
            for (var i = 0; i < spu.Channels.Count; i++)
            {
                var channel = spu.Channels[i];
                if (channel != null) // TODO: remove null check if all channels are implemented.
                    channel.ChannelOutput = Channels[i];
            }
        }
    }
}
