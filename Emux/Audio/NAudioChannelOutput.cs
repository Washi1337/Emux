using System;
using Emux.GameBoy.Audio;
using NAudio.Wave;

namespace Emux.Audio
{
    public class NAudioChannelOutput : BufferedWaveProvider, IAudioChannelOutput
    {
        private readonly GameBoyAudioMixer _mixer;

        public NAudioChannelOutput(GameBoyAudioMixer mixer)
            : base(mixer.WaveFormat)
        {
            if (mixer == null)
                throw new ArgumentNullException(nameof(mixer));
            _mixer = mixer;
            Enabled = true;
        }

        public bool Enabled
        {
            get;
            set;
        }

        public int SampleRate
        {
            get { return WaveFormat.SampleRate; }
        }

        public void BufferSoundSamples(float[] sampleData, int offset, int length)
        {
            byte[] newSampleData = new byte[length * sizeof(float)];
            if (Enabled)
                Buffer.BlockCopy(sampleData, offset * sizeof(float), newSampleData, 0, length * sizeof(float));
            AddSamples(newSampleData, 0, newSampleData.Length);
        }
    }
}