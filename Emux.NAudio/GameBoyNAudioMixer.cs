using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Emux.GameBoy.Audio;
using NAudio.Wave;

namespace Emux.NAudio
{
    public class GameBoyNAudioMixer : IWaveProvider, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly MixingWaveProvider32 _mixer = new MixingWaveProvider32();
        private bool _enabled = true;
        private WaveFileWriter _writer;
        private bool _isRecording;

        public GameBoyNAudioMixer()
        {
            Channels = new List<NAudioChannelOutput>
            {
                new NAudioChannelOutput(this, "Square + Sweep"),
                new NAudioChannelOutput(this, "Square"),
                new NAudioChannelOutput(this, "Wave"),
                new NAudioChannelOutput(this, "Noise"),
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

        public bool IsRecording
        {
            get { return _isRecording; }
            private set
            {
                if (_isRecording != value)
                {
                    _isRecording = value;
                    OnPropertyChanged(nameof(IsRecording));
                }
            }
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

        public int Read(byte[] buffer, int offset, int count)
        {
            _mixer.Read(buffer, offset, count);
            if (!Enabled)
                Array.Clear(buffer, offset, count);

            lock (this)
            {
                _writer?.Write(buffer, offset, count);
            }
            return count;
        }

        public void Connect(GameBoySpu spu)
        {
            for (var i = 0; i < spu.Channels.Count; i++)
            {
                var channel = spu.Channels[i];
                channel.ChannelOutput = Channels[i];
                channel.ChannelVolume = 0.05f;
            }
        }

        public void StartRecording(Stream outputStream)
        {
            if (outputStream == null)
                throw new ArgumentNullException(nameof(outputStream));

            lock (this)
            {
                if (IsRecording)
                    throw new InvalidOperationException("Cannot start a recording when a recording is already happening.");
                _writer = new WaveFileWriter(outputStream, WaveFormat);
                IsRecording = true;
            }
        }

        public void StopRecording()
        {
            lock (this)
            {
                if (!IsRecording)
                    throw new InvalidOperationException("Cannot stop a recording when a recording is not happening.");
                try
                {
                    _writer.Flush();
                }
                finally
                {
                    _writer.Dispose();
                    _writer = null;
                    IsRecording = false;
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
