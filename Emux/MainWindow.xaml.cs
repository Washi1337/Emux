using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Emux.GameBoy.Cartridge;
using Emux.GameBoy.Cpu;
using Microsoft.Win32;

namespace Emux
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly RoutedUICommand StepCommand = new RoutedUICommand(
            "Step",
            "Step",
            typeof(MainWindow),
            new InputGestureCollection(new[]
            {
                new KeyGesture(Key.F10)
            }));

        public static readonly RoutedUICommand RunCommand = new RoutedUICommand(
            "Run",
            "Run",
            typeof(MainWindow),
            new InputGestureCollection(new[]
            {
                new KeyGesture(Key.F5)
            }));

        public static readonly RoutedUICommand BreakCommand = new RoutedUICommand(
            "Break",
            "Break",
            typeof(MainWindow),
            new InputGestureCollection(new[]
            {
                new KeyGesture(Key.F5, ModifierKeys.Control)
            }));

        public static readonly RoutedUICommand SetBreakpointCommand = new RoutedUICommand(
            "Set Breakpoint",
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
            "Reset",
            "Reset",
            typeof(MainWindow));

        public static readonly RoutedUICommand KeyPadCommand = new RoutedUICommand(
            "Keypad",
            "Keypad",
            typeof(MainWindow));

        private GameBoy.GameBoy _gameBoy;
        private readonly VideoWindow _videoWindow;
        private readonly KeypadWindow _keypadWindow;

        public MainWindow()
        {
            InitializeComponent();
            _videoWindow = new VideoWindow();
            _keypadWindow = new KeypadWindow();
        }

        public void RefreshView()
        {
            RegistersTextBox.Text = _gameBoy.Cpu.Registers + "\r\nTick: " + _gameBoy.Cpu.TickCount + "\r\n\r\n" +
                                    "LCDC: " + ((byte)_gameBoy.Gpu.Lcdc).ToString("X2") + "\r\n" +
                                    "STAT: " + ((byte)_gameBoy.Gpu.Stat).ToString("X2") + "\r\n" +
                                    "LY: " + _gameBoy.Gpu.LY.ToString("X2") + "\r\n" +
                                    "ScY: " + _gameBoy.Gpu.ScY.ToString("X2") + "\r\n" +
                                    "ScX: " + _gameBoy.Gpu.ScX.ToString("X2") + "\r\n"
                ;
            DisassemblyView.Items.Clear();
            var disassembler = new Z80Disassembler(_gameBoy.Memory);
            disassembler.Position = _gameBoy.Cpu.Registers.PC;
            for (int i = 0; i < 30 && disassembler.Position < 0xFFFF; i ++)
            {
                var instruction = disassembler.ReadNextInstruction();
                DisassemblyView.Items.Add(instruction.ToString());
            }
            
        }

        private void OpenCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                _gameBoy?.Terminate();
                _gameBoy = new GameBoy.GameBoy(new EmulatedCartridge(File.ReadAllBytes(dialog.FileName)));
                _gameBoy.Cpu.Paused += GameBoyOnPaused;
                _gameBoy.Gpu.VideoOutput = _videoWindow;

                _videoWindow.Device = _gameBoy;
                _videoWindow.Show();
                _keypadWindow.Device = _gameBoy;

                RefreshView();
            }
        }

        private void GameBoyOnPaused(object sender, EventArgs eventArgs)
        {
            Dispatcher.Invoke(RefreshView);
        }

        private void StepCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _gameBoy.Cpu.Step();
            RefreshView();
        }

        private void RunningOnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _gameBoy != null && _gameBoy.Cpu.Running;
        }

        private void PausingOnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _gameBoy != null && !_gameBoy.Cpu.Running;
        }

        private void GameBoyExistsCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _gameBoy != null;
        }

        private void RunCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _gameBoy.Cpu.Run();
        }

        private void BreakCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _gameBoy.Cpu.Break();
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
                        _gameBoy.Cpu.Breakpoints.Add(address);
                    }
                }
            }
        }

        private void ResetCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _gameBoy.Reset();
            RefreshView();
        }

        private void ClearBreakpointsCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _gameBoy.Cpu.Breakpoints.Clear();
        }

        private void KeyPadCommandOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _keypadWindow.Show();
        }
    }
}
