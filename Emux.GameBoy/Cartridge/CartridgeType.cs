using System.Collections;

namespace Emux.GameBoy.Cartridge
{
    public enum CartridgeType : byte
    {
        RomOnly = 0x00,
        Mbc1 = 0x01,
        Mbc1Ram = 0x02,
        Mbc1RamBattery = 0x03,
        Mbc2 = 0x05,
        Mbc2Battery = 0x06,
        RomRam = 0x8,
        RomRamBattery = 0x9,
        Mmm01 = 0xB,
        Mmm01Ram = 0xC,
        Mmm01RamBattery = 0xD,
        Mbc3TimerBattery = 0xF,
        Mbc3TimerRamBattery = 0x10,
        Mbc3 = 0x11,
        Mbc3Ram = 0x12,
        Mbc3RamBattery = 0x13,
        Mbc4 = 0x15,
        Mbc4Ram = 0x16,
        Mbc4RamBattery = 0x17,
        Mbc5 = 0x19,
        Mbc5Ram = 0x1A,
        Mbc5RamBattery = 0x1B,
        Mbc5Rumble = 0x1C,
        Mbc5RumbleRam = 0x1D,
        Mbc5RumbleRamBattery = 0x1E,
        PocketCamera = 0xFC,
        BandaiTama5 = 0xFD,
        HuC3 = 0xFE,
        HuC1RamBattery = 0xFF
    }

    public static class CartridgeTypeExtensions
    {
        public static bool IsRom(this CartridgeType type)
        {
            switch (type)
            {
                case CartridgeType.RomOnly:
                case CartridgeType.RomRam:
                case CartridgeType.RomRamBattery:
                    return true;
            }
            return false;
        }

        public static bool IsMbc1(this CartridgeType type)
        {
            switch (type)
            {
                case CartridgeType.Mbc1:
                case CartridgeType.Mbc1Ram:
                case CartridgeType.Mbc1RamBattery:
                    return true;
            }
            return false;
        }

        public static bool IsMbc2(this CartridgeType type)
        {
            switch (type)
            {
                case CartridgeType.Mbc2:
                case CartridgeType.Mbc2Battery:
                    return true;
            }
            return false;
        }

        public static bool IsMbc3(this CartridgeType type)
        {
            switch (type)
            {
                case CartridgeType.Mbc3:
                case CartridgeType.Mbc3Ram:
                case CartridgeType.Mbc3RamBattery:
                case CartridgeType.Mbc3TimerBattery:
                case CartridgeType.Mbc3TimerRamBattery:
                    return true;
            }
            return false;
        }

        public static bool IsMbc4(this CartridgeType type)
        {
            switch (type)
            {
                case CartridgeType.Mbc4:
                case CartridgeType.Mbc4Ram:
                case CartridgeType.Mbc4RamBattery:
                    return true;
            }
            return false;
        }

        public static bool IsMbc5(this CartridgeType type)
        {
            switch (type)
            {
                case CartridgeType.Mbc5:
                case CartridgeType.Mbc5Ram:
                case CartridgeType.Mbc5RamBattery:
                case CartridgeType.Mbc5Rumble:
                case CartridgeType.Mbc5RumbleRam:
                case CartridgeType.Mbc5RumbleRamBattery:
                    return true;
            }
            return false;
        }

        public static bool HasRam(this CartridgeType type)
        {
            switch (type)
            {
                case CartridgeType.Mbc1Ram:
                case CartridgeType.Mbc3Ram:
                case CartridgeType.Mbc3RamBattery:
                case CartridgeType.Mbc3TimerRamBattery:
                case CartridgeType.Mbc4Ram:
                case CartridgeType.Mbc4RamBattery:
                case CartridgeType.Mbc5Ram:
                case CartridgeType.Mbc5RamBattery:
                case CartridgeType.Mbc5RumbleRam:
                case CartridgeType.Mbc5RumbleRamBattery:
                case CartridgeType.HuC1RamBattery:
                case CartridgeType.RomRam:
                case CartridgeType.Mmm01Ram:
                case CartridgeType.Mmm01RamBattery:
                    return true;
            }
            return false;
        }

        public static bool HasBattery(this CartridgeType type)
        {
            switch (type)
            {
                case CartridgeType.RomRamBattery:
                case CartridgeType.Mbc1RamBattery:
                case CartridgeType.Mbc2Battery:
                case CartridgeType.Mbc3RamBattery:
                case CartridgeType.Mbc3TimerBattery:
                case CartridgeType.Mbc3TimerRamBattery:
                case CartridgeType.Mbc4RamBattery:
                case CartridgeType.Mbc5RamBattery:
                case CartridgeType.Mmm01RamBattery:
                case CartridgeType.HuC1RamBattery:
                    return true;
            }
            return false;
        }

        public static bool HasTimer(this CartridgeType type)
        {
            switch (type)
            {
                case CartridgeType.Mbc3TimerBattery:
                case CartridgeType.Mbc3TimerRamBattery:
                    return true;
            }
            return false;
        }

        public static bool HasRumble(this CartridgeType type)
        {
            switch (type)
            {
                case CartridgeType.Mbc5Rumble:
                case CartridgeType.Mbc5RumbleRam:
                case CartridgeType.Mbc5RumbleRamBattery:
                    return true;
            }
            return false;
        }
    }
}
