using System;

namespace Emux.GameBoy.Graphics
{
    /// <summary>
    /// Provides members for representing the valid LCD control flags used by the LCDC register.
    /// </summary>
    [Flags]
    public enum LcdControlFlags : byte
    {
        /// <summary>
        /// When this bit is set, the GPU will render the background upon each scan line.
        /// </summary>
        EnableBackground = 1 << 0,

        /// <summary>
        /// When this bit is set, the GPU will render sprites upon each scan line.
        /// </summary>
        EnableSprites = 1 << 1,

        /// <summary>
        /// When this bit is set, the GPU will interpret sprites as 8x16 images instead of 8x8.
        /// </summary>
        Sprite8By16Mode = 1 << 2,

        /// <summary>
        /// When this bit is set, the GPU will use 0x9C00 as the start address for reading tile indices of the background. 
        /// When this bit is reset, the starting address is 0x9800.
        /// </summary>
        BgTileMapSelect = 1 << 3,

        /// <summary>
        /// When this bit is set, the GPU will use 0x8000 as the start address for reading tile data of the background. Tile indices are unsigned numbers ranging from 0 to 256. When this bit is reset, the starting address is 0x8800 and the indices range from -128 to 127.
        /// </summary>
        BgWindowTileDataSelect = 1 << 4,

        /// <summary>
        /// When this bit is set, the GPU will render the window layer.
        /// </summary>
        EnableWindow = 1 << 5,

        /// <summary>
        /// When this bit is set, the GPU will use 0x9C00 as the start address for reading tile indices of the window. 
        /// When this bit is reset, the starting address is 0x9800.
        /// </summary>
        WindowTileMapSelect = 1 << 6,

        /// <summary>
        /// When this bit is set, the GPU will be enabled, otherwise disabled.
        /// </summary>
        EnableLcd = 1 << 7
    }
}
