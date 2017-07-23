using System;
using System.Runtime.InteropServices;

namespace Emux.GameBoy.Graphics
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SpriteData
    {
        public byte Y;
        public byte X;
        public byte TileDataIndex;
        public SpriteDataFlags Flags;
    }

    [Flags]
    public enum SpriteDataFlags : byte
    {
        None = 0,
        UsePalette1 = (1 << 4),
        XFlip = (1 << 5),
        YFlip = (1 << 6),
        BelowBackground = (1 << 7)
    }
}
