using System;

namespace Emux.GameBoy.Graphics
{
    [Flags]
    public enum LcdControlFlags : byte
    {
        EnableBgAndWindow = 1 << 0,
        EnableSprites = 1 << 1,
        Sprite8By16Mode = 1 << 2,
        BgTileMapSelect = 1 << 3,
        BgWindowTileDataSelect = 1 << 4,
        EnableWindow = 1 << 5,
        WindowTileMapSelect = 1 << 6,
        EnableLcd = 1 << 7
    }
}
