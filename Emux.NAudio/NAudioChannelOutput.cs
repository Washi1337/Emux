using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Emux.GameBoy.Audio;
using NAudio.Wave;

namespace Emux.NAudio
{
    public class NAudioChannelOutput : BufferedWaveProvider, IAudioChannelOutput, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly GameBoyNAudioMixer _mixer;
        private bool _enabled;

        public NAudioChannelOutput(GameBoyNAudioMixer mixer, string name)
            : base(mixer.WaveFormat)
        {
            if (mixer == null)
                throw new ArgumentNullException(nameof(mixer));
            _mixer = mixer;
            Name = name;
            Enabled = true;
            
        }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    OnPropertyChanged(nameof(Enabled));
                }
            }
        }

        public string Name
        {
            get;
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
        
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}