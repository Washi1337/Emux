using System;
using System.IO;
using Emux.Audio;
using Emux.GameBoy.Cartridge;
using NAudio.Wave;

namespace Emux
{
    public class DeviceManager
    {
        public event EventHandler<DeviceEventArgs> DeviceLoaded;
        public event EventHandler<DeviceEventArgs> DeviceUnloaded;
        public event EventHandler DeviceChanged;

        private GameBoy.GameBoy _currentDevice;
        private StreamedExternalMemory _currentExternalMemory;

        public DeviceManager()
        {
            AudioMixer = new GameBoyAudioMixer();
            var player = new DirectSoundOut();
            player.Init(AudioMixer);
            player.Play();
        }

        public GameBoyAudioMixer AudioMixer
        {
            get;
        }

        public GameBoy.GameBoy CurrentDevice
        {
            get { return _currentDevice; }
            private set
            {
                if (_currentDevice != value)
                {
                    _currentDevice = value;
                    if (value != null)
                    {
                        AudioMixer.Connect(value.Spu);
                    }
                    OnDeviceChanged();
                }
            }
        }

        public void UnloadDevice()
        {
            var device = _currentDevice;
            if (device != null)
            {
                device.Terminate();
                _currentExternalMemory.Dispose();
                _currentDevice = null;
                OnDeviceUnloaded(new DeviceEventArgs(device));
            }
        }

        public void LoadDevice(string romFilePath, string ramFilePath)
        {
            UnloadDevice();
            _currentExternalMemory = new StreamedExternalMemory(File.Open(ramFilePath, FileMode.OpenOrCreate));
            var cartridge = new EmulatedCartridge(File.ReadAllBytes(romFilePath), _currentExternalMemory);
            CurrentDevice = new GameBoy.GameBoy(cartridge, true);
            OnDeviceLoaded(new DeviceEventArgs(CurrentDevice));
        }

        protected virtual void OnDeviceChanged()
        {
            DeviceChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDeviceLoaded(DeviceEventArgs e)
        {
            DeviceLoaded?.Invoke(this, e);
        }

        protected virtual void OnDeviceUnloaded(DeviceEventArgs e)
        {
            DeviceUnloaded?.Invoke(this, e);
        }
    }
}
