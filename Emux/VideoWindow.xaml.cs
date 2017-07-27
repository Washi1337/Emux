using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emux.GameBoy.Graphics;
using Emux.GameBoy.Input;

namespace Emux
{
    /// <summary>
    /// Interaction logic for VideoWindow.xaml
    /// </summary>
    public partial class VideoWindow : IVideoOutput
    {
        private readonly WriteableBitmap _bitmap = new WriteableBitmap(GameBoyGpu.FrameWidth, GameBoyGpu.FrameHeight, 96, 96, PixelFormats.Bgr24, null);
        private readonly IDictionary<Key, GameBoyPadButton> _keyMapping = new Dictionary<Key, GameBoyPadButton>()
        {
            [Key.Up] = GameBoyPadButton.Up,
            [Key.Down] = GameBoyPadButton.Down,
            [Key.Left] = GameBoyPadButton.Left,
            [Key.Right] = GameBoyPadButton.Right,
            [Key.X] = GameBoyPadButton.A,
            [Key.Z] = GameBoyPadButton.B,
            [Key.Enter] = GameBoyPadButton.Start,
            [Key.LeftShift] = GameBoyPadButton.Select
        };
        private readonly Timer _frameRateTimer = new Timer(1000);

        private GameBoy.GameBoy _device;
        
        public VideoWindow()
        {
            InitializeComponent();
            _frameRateTimer.Start();
            _frameRateTimer.Elapsed += FrameRateTimerOnElapsed;
        }

        private void FrameRateTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (Device != null)
            {
                lock (this)
                {
                    Dispatcher.Invoke(() => Title = string.Format("GameBoy Video Output ({0:0.00} FPS)",
                        _device.Cpu.FramesPerSecond));
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
            GameBoyPadButton button;
            if (_keyMapping.TryGetValue(e.Key, out button))
                Device.KeyPad.PressedButtons |= button;
        }


        private void VideoWindowOnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                Device.Cpu.EnableFrameLimit = true;
            GameBoyPadButton button;
            if (_keyMapping.TryGetValue(e.Key, out button))
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
