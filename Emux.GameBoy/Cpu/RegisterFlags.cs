using System;

namespace Emux.GameBoy.Cpu
{
    /// <summary>
    /// Provides members for representing all the register flags known by the GameBoy CPU.
    /// </summary>
    [Flags]
    public enum RegisterFlags : byte
    {
        None = 0,
        Z = 1 << 7,
        N = 1 << 6,
        H = 1 << 5,
        C = 1 << 4,
        All = Z | N | H | C
    }
}