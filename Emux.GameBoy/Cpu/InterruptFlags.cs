using System;

namespace Emux.GameBoy.Cpu
{
    /// <summary>
    /// Provides valid interrupt flags used by the interrupt registers IF and IE.
    /// </summary>
    [Flags]
    public enum InterruptFlags : byte
    {
        None = 0,
        VBlank = (1 << 0),
        LcdStat = (1 << 1),
        Timer = (1 << 2),
        Serial = (1 << 3),
        Joypad = (1 << 4)
    }
}
