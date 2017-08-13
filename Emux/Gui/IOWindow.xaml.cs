using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Emux.GameBoy.Graphics;

namespace Emux.Gui
{
    /// <summary>
    /// Interaction logic for IOWindow.xaml
    /// </summary>
    public partial class IOWindow : Window
    {
        private GameBoy.GameBoy _device;

        public IOWindow()
        {
            InitializeComponent();
        }

        public GameBoy.GameBoy Device
        {
            get { return _device; }
            set
            {
                lock (this)
                {
                    if (_device != value)
                    {
                        if (value != null)
                        {
                            value.Cpu.Paused -= CpuOnPaused;
                            value.Cpu.Resumed -= CpuOnResumed;
                        }
                        _device = value;
                        DisabledOverlay.DisableOverlay();
                        if (_device != null)
                        {
                            _device.Cpu.Paused += CpuOnPaused;
                            _device.Cpu.Resumed += CpuOnResumed;
                        }
                    }
                }
            }
        }

        private void CpuOnPaused(object sender, EventArgs eventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                if (IsVisible)
                    RefreshView();
                DisabledOverlay.DisableOverlay();
            });
        }

        private void CpuOnResumed(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                DisabledOverlay.EnableOverlay(500);
            });
        }

        public void RefreshView()
        {
            LcdcFlagsListBox.RawValue = (byte) Device.Gpu.Lcdc;
            StatFlagsListBox.RawValue = (byte) Device.Gpu.Stat;

            foreach (var item in GpuRegistersView.Items.Cast<RegisterItem>())
                item.Value = Device.Memory.ReadByte(item.Offset);
        }

        private void IOWindowOnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = Device != null;
            Hide();
        }

        private void CommandBindingOnCanAlwaysExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CloseCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void RefreshCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            RefreshView();
        }

        private void LcdcFlagsListBoxOnRawValueChanged(object sender, EventArgs e)
        {
            Device.Gpu.Lcdc = (LcdControlFlags) LcdcFlagsListBox.RawValue;
            GpuRegistersView.Items.Cast<RegisterItem>().First(x => x.DisplayName == "LCDC").Value =
                LcdcFlagsListBox.RawValue;
        }

        private void IOWindowOnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
                RefreshView();
        }

        private void StatFlagsListBoxOnRawValueChanged(object sender, EventArgs e)
        {
            Device.Gpu.Stat = (LcdStatusFlags)StatFlagsListBox.RawValue;
            GpuRegistersView.Items.Cast<RegisterItem>().First(x => x.DisplayName == "STAT").Value =
                StatFlagsListBox.RawValue;
        }

        private void GpuRegistersViewOnItemActivate(object sender, EventArgs e)
        {
            if (GpuRegistersView.SelectedItem == null)
                return;

            var item = (RegisterItem) GpuRegistersView.SelectedItem;
            string text = item.Value.ToString("X2");
            bool repeat = true;
            while (repeat)
            {
                var dialog = new InputDialog
                {
                    Title = "Enter new value for " + item.DisplayName,
                    Text = text
                };
                var result = dialog.ShowDialog();
                repeat = result.HasValue && result.Value;
                if (repeat)
                {
                    byte newValue;
                    text = dialog.Text;
                    repeat = !byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out newValue);

                    if (repeat)
                    {
                        MessageBox.Show("Please enter a valid hexadecimal number between 00 and FF", "Emux",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        item.Value = newValue;
                    }
                }
            }
        }
    }
}
