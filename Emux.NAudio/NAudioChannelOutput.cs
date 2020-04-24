using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Emux.GameBoy.Audio;
using NAudio.Wave;

namespace Emux.NAudio
{
    public class NAudioChannelOutput : BufferedWaveProvider, IAudioChannelOutput, INotifyPropertyChanged
    {
        private static readonly byte[] _floatBuffer = new byte[sizeof(float)];
        private readonly byte[] _newSampleData;

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

            _newSampleData = new byte[BufferLength];
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

        public void BufferSoundSamples(Span<float> sampleData, int offset, int length)
        {
            if (Enabled)
            {
                for (int i = 0, j = 0; j<offset+length; j++)
                {
                    GetBytes(sampleData[j], _floatBuffer);
                    foreach (var b in _floatBuffer)
                        _newSampleData[i++] = b;
                }
            }
            AddSamples(_newSampleData, 0, length * sizeof(float));
        }

        [System.Security.SecuritySafeCritical]
        public unsafe static void GetBytes(float value, byte[] bytes) => GetBytes(*(int*)&value, bytes);
        [System.Security.SecuritySafeCritical]
        public unsafe static void GetBytes(int value, byte[] bytes)
        {
            fixed (byte* b = bytes)
                *(int*)b = value;
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}