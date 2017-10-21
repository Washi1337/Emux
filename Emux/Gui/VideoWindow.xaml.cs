using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emux.GameBoy.Graphics;
using Emux.GameBoy.Input;

namespace Emux.Gui
{
    /// <summary>
    /// Interaction logic for VideoWindow.xaml
    /// </summary>
    public partial class VideoWindow : IVideoOutput
    {
        private readonly WriteableBitmap _bitmap = new WriteableBitmap(GameBoyGpu.FrameWidth, GameBoyGpu.FrameHeight, 96, 96, PixelFormats.Rgb24, null);
        private readonly DispatcherTimer _frameRateTimer;

        private GameBoy.GameBoy _device;
        
        public VideoWindow()
        {
            InitializeComponent();
            _frameRateTimer = new DispatcherTimer();
            _frameRateTimer.Tick += FrameRateTimerOnTick;
            _frameRateTimer.Interval = new TimeSpan(0, 0, 1);
            _frameRateTimer.Start();
        }

        private bool GetBindedButton(Key key, out GameBoyPadButton button)
        {
            foreach (var name in Enum.GetValues(typeof(GameBoyPadButton)).Cast<GameBoyPadButton>().Where(x => x != GameBoyPadButton.None))
            {
                var bindedKey = (Key) Properties.Settings.Default["KeyBinding" + name];
                if (bindedKey == key)
                {
                    button = name;
                    return true;
                }
            }
            button = GameBoyPadButton.A;
            return false;
        }

        private void FrameRateTimerOnTick(object sender, EventArgs eventArgs)
        {
            if (Device != null)
            {
                lock (this)
                {
                    Dispatcher.Invoke(() => Title = string.Format("Video Output ({0:0.00} %)",
                        _device.Cpu.SpeedFactor * 100));
                }
            }
        }

        public GameBoy.GameBoy Device
        {
            get { return _device; }
            set
            {
                if (_device != null)
                    _device.Gpu.VideoOutput = new EmptyVideoOutput();
                _device = value;
                if (value != null)
                    Device.Gpu.VideoOutput = this;
            }
        }

        public void RenderFrame(byte[] pixelData)
        {
            Dispatcher.Invoke(() =>
            {
                _bitmap.WritePixels(new Int32Rect(0, 0, 160, 144), pixelData, _bitmap.BackBufferStride, 0);
                VideoImage.Source = _bitmap;
            });
        }

        private void VideoWindowOnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                Device.Cpu.EnableFrameLimit = false;
            else if (GetBindedButton(e.Key, out var button))
                Device.KeyPad.PressedButtons |= button;
        }


        private void VideoWindowOnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                Device.Cpu.EnableFrameLimit = true;
            else if (GetBindedButton(e.Key, out var button))
                Device.KeyPad.PressedButtons &= ~button;
        }

        private void VideoWindowOnClosing(object sender, CancelEventArgs e)
        {
            lock (this)
            {
                _frameRateTimer.Stop();
                e.Cancel = Device != null;
                Hide();
            }
        }
    }
}
