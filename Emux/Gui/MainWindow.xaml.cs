using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Emux.GameBoy.Cpu;
using Microsoft.Win32;

namespace Emux.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly RoutedUICommand StepCommand = new RoutedUICommand(
            "Execute next instruction.",
            "Step",
            typeof(MainWindow),
            new InputGestureCollection(new[]
            {
                new KeyGesture(Key.F10)
            }));

        public static readonly RoutedUICommand RunCommand = new RoutedUICommand(
            "Continue execution.",
            "Run",
            typeof(MainWindow),
            new InputGestureCollection(new[]
            {
                new KeyGesture(Key.F5)
            }));

        public static readonly RoutedUICommand BreakCommand = new RoutedUICommand(
            "Break execution.",
            "Break",
            typeof(MainWindow),
            new InputGestureCollection(new[]
            {
                new KeyGesture(Key.F5, ModifierKeys.Control)
            }));

        public static readonly RoutedUICommand SetBreakpointCommand = new RoutedUICommand(
            "Set an execution breakpoint to a memory address.",
            "Set Breakpoint",
            typeof(MainWindow),
            new InputGestureCollection(new[]
            {
                new KeyGesture(Key.F2)
            }));


        public static readonly RoutedUICommand ClearBreakpointsCommand = new RoutedUICommand(
            "Clear all breakpoints",
            "Clear all breakpoints",
            typeof(MainWindow));

        public static readonly RoutedUICommand ResetCommand = new RoutedUICommand(
            "Reset the GameBoy device.",
            "Reset",
            typeof(MainWindow));

        public static readonly RoutedUICommand VideoOutputCommand = new RoutedUICommand(
            "Open the video output window",
            "Video Output",
            typeof(MainWindow),
            new InputGestureCollection(new[]
            {
                new KeyGesture(Key.F11)
            }));

        public static readonly RoutedUICommand IOMemoryCommand = new RoutedUICommand(
            "Open the IO memory view",
            "IO Memory",
            typeof(MainWindow));

        public static readonly RoutedUICommand KeyPadCommand = new RoutedUICommand(
            "Open the virtual keypad window",
            "Keypad",
            typeof(MainWindow),
            new InputGestureCollection(new[]
            {
                new KeyGesture(Key.F12)
            }));

        public static readonly RoutedUICommand EnableSoundCommand = new RoutedUICommand(
            "Enable or disable sound",
            "Enable sound",
            typeof(MainWindow));

        public static readonly RoutedUICommand SourceCodeCommand = new RoutedUICommand(
            "View the source code of the program.",
            "Source Code",
            typeof(MainWindow));

        public static readonly RoutedUICommand AboutCommand = new RoutedUICommand(
            "View about details.",
            "About",
            typeof(MainWindow),
            new InputGestureCollection(new[]
            {
                new KeyGesture(Key.F1)
            }));

        private readonly VideoWindow _videoWindow;
        private readonly KeypadWindow _keypadWindow;
        private readonly IOWindow _ioWindow;
        private GameBoy.GameBoy _currentDevice;

        public MainWindow()
        {
            InitializeComponent();
            _videoWindow = new VideoWindow();
            _keypadWindow = new KeypadWindow();
            _ioWindow = new IOWindow();

            App.Current.DeviceManager.DeviceChanged += DeviceManagerOnDeviceChanged;
        }

        private void DeviceManagerOnDeviceChanged(object sender, EventArgs e)
        {
            if (_currentDevice != null)
            {
                _currentDevice.Cpu.Paused -= GameBoyOnPaused;
                _currentDevice.Cpu.Resumed -= GameBoyOnResumed;
                RunningOverlay.DisableOverlay();
            }

            _currentDevice = App.Current.DeviceManager.CurrentDevice;
            _currentDevice.Cpu.Paused += GameBoyOnPaused;
            _currentDevice.Cpu.Resumed += GameBoyOnResumed;
            _currentDevice.Gpu.VideoOutput = _videoWindow;

            _videoWindow.Device = _currentDevice;
            _keypadWindow.Device = _currentDevice;
            _ioWindow.Device = _currentDevice;
            
            _videoWindow.Show();

            RefreshView();

            if (EnableSoundsMenuItem.IsChecked)
                _currentDevice.Spu.ActivateAllChannels();
            else
                _currentDevice.Spu.DeactivateAllChannels();
        }

        public void RefreshView()
        {
            RegistersTextBox.Text = _currentDevice.Cpu.Registers + "\r\nTick: " + _currentDevice.Cpu.TickCount + "\r\n\r\n" +
                                    "LCDC: " + ((byte) _currentDevice.Gpu.Lcdc).ToString("X2") + "\r\n" +
                                    "STAT: " + ((byte) _currentDevice.Gpu.Stat).ToString("X2") + "\r\n" +
                                    "LY: " + _currentDevice.Gpu.LY.ToString("X2") + "\r\n" +
                                    "ScY: " + _currentDevice.Gpu.ScY.ToString("X2") + "\r\n" +
                                    "ScX: " + _currentDevice.Gpu.ScX.ToString("X2") + "\r\n" +
                                    "\r\n" +
                                    "TIMA: " + _currentDevice.Timer.Tima.ToString("X2") + "\r\n" +
                                    "TMA: " + _currentDevice.Timer.Tma.ToString("X2") + "\r\n" +
                                    "TAC: " + ((byte) _currentDevice.Timer.Tac).ToString("X2") + "\r\n";
                ;
            DisassemblyView.Items.Clear();
            var disassembler = new Z80Disassembler(_currentDevice.Memory);
            disassembler.Position = _currentDevice.Cpu.Registers.PC;
            for (int i = 0; i < 30 && disassembler.Position < 0xFFFF; i ++)
            {
                var instruction = disassembler.ReadNextInstruction();
                DisassemblyView.Items.Add(new InstructionItem(_currentDevice, instruction));
            }
            
        }

        private void OpenCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "GameBoy ROM file (*.gb)|*.gb";
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
                App.Current.DeviceManager.LoadDevice(dialog.FileName, Path.ChangeExtension(dialog.FileName, ".sav"));
        }

        private void GameBoyOnPaused(object sender, EventArgs eventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                RefreshView();
                RunningOverlay.DisableOverlay();
            });
        }

        private void GameBoyOnResumed(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                RunningOverlay.EnableOverlay(500);
            });
        }

        private void StepCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _currentDevice.Cpu.Step();
            RefreshView();
        }

        private void RunningOnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _currentDevice != null && _currentDevice.Cpu.Running;
        }

        private void PausingOnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _currentDevice != null && !_currentDevice.Cpu.Running;
        }

        private void GameBoyExistsCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _currentDevice != null;
        }

        private void RunCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _currentDevice.Cpu.Run();
        }

        private void BreakCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _currentDevice.Cpu.Break();
        }

        private void SetBreakpointCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            string text = "0000";
            bool repeat = true;
            while (repeat)
            {
                var dialog = new InputDialog
                {
                    Title = "Enter breakpoint address",
                    Text = text
                };
                var result = dialog.ShowDialog();
                repeat = result.HasValue && result.Value;
                if (repeat)
                {
                    ushort address;
                    text = dialog.Text;
                    repeat = !ushort.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address);

                    if (repeat)
                    {
                        MessageBox.Show("Please enter a valid hexadecimal number between 0000 and FFFF", "Emux",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        _currentDevice.Cpu.Breakpoints.Add(address);
                    }
                }
            }
        }

        private void ResetCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _currentDevice.Reset();
            RefreshView();
        }

        private void ClearBreakpointsCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _currentDevice.Cpu.Breakpoints.Clear();
        }

        private void KeyPadCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _keypadWindow.Show();
        }

        private void SourceCodeCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Process.Start(Properties.Settings.Default.Repository);
        }

        private void AboutCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            new AboutDialog().ShowDialog();
        }

        private void VideoOutputCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _videoWindow.Show();
            if (_videoWindow.WindowState == WindowState.Minimized)
                _videoWindow.WindowState = WindowState.Normal;
            _videoWindow.Focus();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            App.Current.DeviceManager.UnloadDevice();
            _videoWindow.Device = null;
            _keypadWindow.Device = null;
            _ioWindow.Device = null;
            _videoWindow.Close();
            _keypadWindow.Close();
            _ioWindow.Close();
        }

        private void EnableSoundCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (EnableSoundsMenuItem.IsChecked)
                _currentDevice.Spu.ActivateAllChannels();
            else
                _currentDevice.Spu.DeactivateAllChannels();
        }

        private void IOMemoryCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _ioWindow.Show();
        }
    }
}
