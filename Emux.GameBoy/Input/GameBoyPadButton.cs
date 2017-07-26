using System;

namespace Emux.GameBoy.Input
{
    /// <summary>
    /// Provides members that represent all the buttons on a GameBoy device.
    /// </summary>
    [Flags]
    public enum GameBoyPadButton : byte
    {
        None = 0,

        Right = 0x01,
        Left = 0x02,
        Up = 0x04,
        Down = 0x08,

        A = 0x10,
        B = 0x20,
        Select = 0x40,
        Start = 0x80,
    }
}