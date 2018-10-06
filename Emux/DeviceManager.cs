using System;
using System.Collections.Generic;
using System.IO;
using Emux.GameBoy.Cartridge;
using Emux.GameBoy.Cheating;
using Emux.GameBoy.Graphics;
using Emux.NAudio;
using Emux.Gui;
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
            AudioMixer = new GameBoyNAudioMixer();
            var player = new DirectSoundOut();
            player.Init(AudioMixer);
            player.Play();

            GamesharkController = new GamesharkController();
            Properties.Settings.Default.PropertyChanged += SettingsOnPropertyChanged;
            
            Breakpoints = new Dictionary<ushort, BreakpointInfo>();
        }

        public GameBoyNAudioMixer AudioMixer
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
                        GamesharkController.Device = value;
                    }
                    OnDeviceChanged();
                }
            }
        }

        public GamesharkController GamesharkController
        {
            get;
        }

        public IDictionary<ushort, BreakpointInfo> Breakpoints
        {
            get;
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
            _currentExternalMemory.SetBufferSize(cartridge.ExternalRamSize);
            CurrentDevice = new GameBoy.GameBoy(cartridge, new WinMmTimer(60), !Properties.Settings.Default.ForceOriginalGameBoy);
            ApplyColorPalettes();
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

        private static Color ConvertColor(System.Windows.Media.Color color)
        {
            return new Color(color.R, color.G, color.B);
        }

        private void ApplyColorPalettes()
        {
            if (CurrentDevice != null)
            {
                CurrentDevice.Gpu.Color0 = ConvertColor(Properties.Settings.Default.GBColor0);
                CurrentDevice.Gpu.Color1 = ConvertColor(Properties.Settings.Default.GBColor1);
                CurrentDevice.Gpu.Color2 = ConvertColor(Properties.Settings.Default.GBColor2);
                CurrentDevice.Gpu.Color3 = ConvertColor(Properties.Settings.Default.GBColor3);
            }
        }

        private void SettingsOnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Contains("GBColor"))
                ApplyColorPalettes();
        }
    }
}
