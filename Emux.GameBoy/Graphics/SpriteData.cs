using System;
using System.Runtime.InteropServices;

namespace Emux.GameBoy.Graphics
{
    /// <summary>
    /// Represents the structure of a sprite located in the Object Attribute Memory (OAM).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SpriteData
    {
        public byte Y;
        public byte X;
        public byte TileDataIndex;
        public SpriteDataFlags Flags;
    }

    /// <summary>
    /// Provides members for representing all the valid sprite atrributes.
    /// </summary>
    [Flags]
    public enum SpriteDataFlags : byte
    {
        None = 0,
        PaletteNumberMask = 0b111,
        TileVramBank = (1 << 3),
        UsePalette1 = (1 << 4),
        XFlip = (1 << 5),
        YFlip = (1 << 6),
        BelowBackground = (1 << 7)
    }
}
