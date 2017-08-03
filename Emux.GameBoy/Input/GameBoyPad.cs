using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Input
{
    /// <summary>
    /// Represents the Keypad driver of a GameBoy device.
    /// </summary>
    public class GameBoyPad
    {
        private readonly GameBoy _device;
        private GameBoyPadButton _pressedButtons;
        private byte _joyP;

        public GameBoyPad(GameBoy device)
        {
            _device = device;
        }

        /// <summary>
        /// Gets or sets a value indicating all the buttons that are currently pressed.
        /// </summary>
        public GameBoyPadButton PressedButtons
        {
            get { return _pressedButtons; }
            set
            {
                if (_pressedButtons < value)
                    _device.Cpu.Registers.IF |= InterruptFlags.Joypad;
                _pressedButtons = value; 
            }
        }

        public byte JoyP
        {
            get
            {
                if ((_joyP & 0x10) == 0x10)
                    return (byte)(0xD0 | (~((byte)PressedButtons >> 4) & 0xF));
                if ((_joyP & 0x20) == 0x20)
                    return (byte)(0xE0 | (~(byte)PressedButtons & 0xF));
                return 0xFE;
            }
            set { _joyP = value; }
        }        
    }
}
