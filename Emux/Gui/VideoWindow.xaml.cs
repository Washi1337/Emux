﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
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
        private WriteableBitmap _bitmap;
        private readonly DispatcherTimer _frameRateTimer;

        private GameBoy.GameBoy _device;

        public VideoWindow()
        {
            InitializeComponent();
            _frameRateTimer = new DispatcherTimer();
            _frameRateTimer.Tick += FrameRateTimerOnTick;
            _frameRateTimer.Interval = new TimeSpan(0, 0, 1);
            _frameRateTimer.Start();

            VideoImage.Source = _bitmap;
        }

        public void SetSize(int width, int height)
        {
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            Width = width * 2;
            Height = height * 2;
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
                    Dispatcher.Invoke(() => Title = string.Format("Video Output ({0:0.00}x)",
                        _device.SpeedFactor));
                }
            }
        }

        public GameBoy.GameBoy Device
        {
            get { return _device; }
            set
            {
                _device = value;
            }
        }

        public void RenderFrame(byte[] pixelData)
        {
            // Should really await this but theres no async context here. No idea how it would throw anyway
            Dispatcher.InvokeAsync(() =>
            {
                _bitmap.WritePixels(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight), pixelData, _bitmap.BackBufferStride, 0);
                VideoImage.Source = _bitmap;
            });
        }

        private void VideoWindowOnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                Device.EnableFrameLimit = false;
            else if (GetBindedButton(e.Key, out var button))
                Device.KeyPad.PressedButtons |= button;
        }


        private void VideoWindowOnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                Device.EnableFrameLimit = true;
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

        public void Blit()
        {
           
        }
    }
}
