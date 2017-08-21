using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emux.GameBoy.Cartridge
{
    public enum GameBoyColorFlag : byte
    {
        OriginalGameBoy = 0,
        SupportsColor = 0x80,
        GameBoyColorOnly = 0xC0,
    }
}
