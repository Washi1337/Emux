using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Input
{
    public class GameBoyPad
    {
        private readonly GameBoy _device;
        private GameBoyPadButton _pressedButtons;
        private byte _joyP;

        public GameBoyPad(GameBoy device)
        {
            _device = device;
        }

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
                return 0xF0;
            }
            set { _joyP = value; }
        }        
    }
}
