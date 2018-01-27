using System;

namespace Emux.GameBoy.Audio
{
    [Flags]
    public enum SpuOutputSelection : byte
    {
        OutputChannel1ToS01 = 1 << 0,
        OutputChannel2ToS01 = 1 << 1,
        OutputChannel3ToS01 = 1 << 2,
        OutputChannel4ToS01 = 1 << 3,
        OutputChannel1ToS02 = 1 << 4,
        OutputChannel2ToS02 = 1 << 5,
        OutputChannel3ToS02 = 1 << 6,
        OutputChannel4ToS02 = 1 << 7,
    }
}