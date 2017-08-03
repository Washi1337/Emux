namespace Emux.GameBoy.Audio
{
    public interface ISoundChannel
    { 
        byte NR0
        {
            get;
            set;
        }

        byte NR1
        {
            get;
            set;
        }

        byte NR2
        {
            get;
            set;
        }

        byte NR3
        {
            get;
            set;
        }

        byte NR4
        {
            get;
            set;
        }

        bool Active
        {
            get;
            set;
        }

        IAudioChannelOutput ChannelOutput
        {
            get;
            set;
        }

        void ChannelStep(int cycles);
    }
}