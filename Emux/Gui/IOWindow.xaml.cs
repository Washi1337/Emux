using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Emux.GameBoy.Graphics;

namespace Emux.Gui
{
    /// <summary>
    /// Interaction logic for IOWindow.xaml
    /// </summary>
    public partial class IOWindow : Window
    {
        public static readonly DependencyProperty AutoRefreshProperty = DependencyProperty.Register("AutoRefresh",
            typeof(bool), typeof(IOWindow), new PropertyMetadata((o, e) => ((IOWindow)o)._refreshTimer.IsEnabled = (bool) e.NewValue));

        private GameBoy.GameBoy _device;
        private readonly DispatcherTimer _refreshTimer;
        public IOWindow()
        {
            InitializeComponent();
            _refreshTimer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 50), DispatcherPriority.Background,
                (o, e) =>
                {
                    if (Device != null)
                        RefreshView();
                }, Dispatcher);
            _refreshTimer.Stop();
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
                        if (_device != null)
                        {
                            _device.Cpu.Paused += CpuOnPaused;
                            _device.Cpu.Resumed += CpuOnResumed;
                        }
                    }
                }
            }
        }
        public bool AutoRefresh
        {
            get { return (bool) GetValue(AutoRefreshProperty); }
            set { SetValue(AutoRefreshProperty, value); }
        }

        private void CpuOnPaused(object sender, EventArgs eventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                if (IsVisible)
                    RefreshView();
            });
        }

        private void CpuOnResumed(object sender, EventArgs e)
        {
        }

        public void RefreshView()
        {
            LcdcFlagsListBox.RawValue = (byte) Device.Gpu.Lcdc;
            StatFlagsListBox.RawValue = (byte) Device.Gpu.Stat;

            foreach (var item in GpuRegistersView.Items.Cast<RegisterItem>())
                item.Value = Device.Memory.ReadByte(item.Offset);
            foreach (var item in Sound1RegistersView.Items.Cast<RegisterItem>())
                item.Value = Device.Memory.ReadByte(item.Offset);
            foreach (var item in Sound2RegistersView.Items.Cast<RegisterItem>())
                item.Value = Device.Memory.ReadByte(item.Offset);
            foreach (var item in Sound3RegistersView.Items.Cast<RegisterItem>())
                item.Value = Device.Memory.ReadByte(item.Offset);
            foreach (var item in Sound4RegistersView.Items.Cast<RegisterItem>())
                item.Value = Device.Memory.ReadByte(item.Offset);
            foreach (var item in MasterSoundRegistersView.Items.Cast<RegisterItem>())
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

        private void RegistersViewOnItemActivate(object sender, EventArgs e)
        {
            var listView = (ListView) sender;
            if (listView.SelectedItem == null)
                return;

            var item = (RegisterItem) listView.SelectedItem;
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
                        Device.Memory.WriteByte(item.Offset, newValue);
                    }
                }
            }
        }
    }
}
