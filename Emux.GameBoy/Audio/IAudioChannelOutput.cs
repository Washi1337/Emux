namespace Emux.GameBoy.Audio
{
    public interface IAudioChannelOutput
    {
        int SampleRate
        {
            get;
        }

        void BufferSoundSamples(float[] sampleData, int offset, int length);
    }
}
