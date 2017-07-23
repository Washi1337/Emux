using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Emux.GameBoy.Input;

namespace Emux
{
    /// <summary>
    /// Interaction logic for KeypadDialog.xaml
    /// </summary>
    public partial class KeypadWindow : Window
    {
        private readonly IDictionary<CheckBox, GameBoyPadButton> _keypadMapping = new Dictionary<CheckBox, GameBoyPadButton>();
        private GameBoy.GameBoy _device;

        public KeypadWindow()
        {
            InitializeComponent();
            
            _keypadMapping[UpCheckBox] = GameBoyPadButton.Up;
            _keypadMapping[DownCheckBox] = GameBoyPadButton.Down;
            _keypadMapping[LeftCheckBox] = GameBoyPadButton.Left;
            _keypadMapping[RightCheckBox] = GameBoyPadButton.Right;
            _keypadMapping[ACheckBox] = GameBoyPadButton.A;
            _keypadMapping[BCheckBox] = GameBoyPadButton.B;
            _keypadMapping[StartCheckBox] = GameBoyPadButton.Start;
            _keypadMapping[SelectCheckBox] = GameBoyPadButton.Select;
        }

        public GameBoy.GameBoy Device
        {
            get { return _device; }
            set { _device = value; }
        }
        
        private void ButtonCheckBoxOnChecked(object sender, RoutedEventArgs e)
        {
            _device.KeyPad.PressedButtons |= _keypadMapping[(CheckBox) sender];
        }

        private void ButtonCheckBoxOnUnchecked(object sender, RoutedEventArgs e)
        {
            _device.KeyPad.PressedButtons &= ~_keypadMapping[(CheckBox) sender];
        }

        private void KeypadWindowOnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = Device == null;
            Hide();
        }
    }
}
