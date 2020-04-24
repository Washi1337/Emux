using System;

namespace Emux.GameBoy.Audio
{
    public interface IAudioChannelOutput
    {
        int SampleRate
        {
            get;
        }

        void BufferSoundSamples(Span<float> sampleData, int offset, int length);
    }
}
