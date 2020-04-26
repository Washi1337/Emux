using System;

namespace Emux.GameBoy.Graphics
{
    /// <summary>
    /// Provides members for representing all valid LCD status flags provided by the STAT register.
    /// </summary>
    [Flags]
    public enum LcdStatusFlags : byte
    {
        /// <summary>
        /// Specifies the GPU is finishing up a horizontal blank and moving to the next scan line.
        /// </summary>
        HBlankMode = 0,
        
        /// <summary>
        /// Specifies the GPU is in VBlank mode.
        /// </summary>
        VBlankMode = 1,

        /// <summary>
        /// Specifies the GPU is scanning the Object Attribute Memory (OAM).
        /// </summary>
        ScanLineOamMode = (1 << 1),
        
        /// <summary>
        /// Specifies the GPU is scanning the Video Memory.
        /// </summary>
        ScanLineVRamMode = 0b11,

        /// <summary>
        /// Provides a bitmask for getting the current GPU mode.
        /// </summary>
        ModeMask = 0b11,

        /// <summary>
        /// When this bit is set, the LY register equals the LYC register.
        /// </summary>
        Coincidence = (1 << 2),

        /// <summary>
        /// When this bit is set, HBlank interrupts are enabled.
        /// </summary>
        HBlankModeInterrupt = (1 << 3),

        /// <summary>
        /// When this bit is set, VBlank interrupts are enabled.
        /// </summary>
        VBlankModeInterrupt = (1 << 4),

        /// <summary>
        /// When this bit is set, OAM scan interrupts are enabled.
        /// </summary>
        OamBlankModeInterrupt = (1 << 5),

        /// <summary>
        /// When this bit is set, VRAM scan interrupts are enabled.
        /// </summary>
        CoincidenceInterrupt = (1 << 6),
    }
}
