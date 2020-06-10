using System;
using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Memory
{
    /// <summary>
    /// Represents the memory controller of a GameBoy device.
    /// </summary>
    public class GameBoyMemory : IGameBoyComponent
    {
        public static readonly ushort
            VRAMStartAddress = 0x8000,
            OAMDMABlockSize = 16,
            SpriteSize = 4,
            NumSprites = 40,
            OAMSize = (ushort)(SpriteSize * NumSprites);
        public const int
            ROMBank0 = 0x0000,
            ROMBankN = 0x4000,
            VRAM = 0x8000,
            ExternalRAM = 0xA000,
            WorkRAMBank0 = 0xC000,
            WorkRAMBankN = 0xD000,
            EchoRAM = 0xC000,
            IORegisters = 0xFF00,
            OAMLocation = 0xFE00,
            HighRAMLocation = 0xFF80;

        private readonly GameBoy _device;

        private readonly byte[] _internalRam = new byte[0x1000];
        private readonly byte[] _internalSwitchableRam;
        private int _internalRamBankIndex = 1;
        private readonly byte[] _highInternalRam = new byte[0x7F];
        // Most instructions are 1 or 2 oprands, reuse these buffers if so
        private readonly byte[] _singleOprandBuffer = new byte[1];
        private readonly byte[] _doubleOprandBuffer = new byte[2];

        // TODO: to be removed:
        private readonly byte[] _io = new byte[4];

        public GameBoyMemory(GameBoy device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            _device = device;
            _internalSwitchableRam = new byte[device.GbcMode ? 0x7000 : 0x1000];
            DmaController = new DmaController(device);
        }

        public DmaController DmaController { get; }

        // Another component is actively using RAM so RAM is unavailable (except HRAM)
        public bool RAMIsBusy { get; set; }

        // Another component is actively using ROM so RoM is unavailable. CPU is essentially halted.
        public bool ROMIsBusy { get; set; }

        public void Initialize()
        {
            DmaController.Initialize();
        }

        public void Reset()
        {
            SwitchRamBank(1);
            DmaController.Reset();
        }

        public void Shutdown()
        {
            DmaController.Shutdown();
        }

        public byte ReadByte(ushort address, bool lockBus = true)
        {

            if (_device.Memory.RAMIsBusy && lockBus)
            {
                if (address >= HighRAMLocation && address != 0xFFFF)
                {
                    return _highInternalRam[address - HighRAMLocation];
                }
                else
                {
                    return 0xFF;
                }
            }

            switch (address >> 12)
            {
                case 0x0: // rom (0x0000 -> 0x3FFF)
                case 0x1:
                case 0x2:
                case 0x3:
                case 0x4: // switchable rom (0x4000 -> 0x7FFF)
                case 0x5:
                case 0x6:
                case 0x7:
                    return _device.Cartridge.ReadByte(address);
                case 0x8: // vram (0x8000 -> 0x9FFF)
                case 0x9:
                    if ((_device.Gpu.LCDMode & Graphics.LcdStatusFlags.ScanLineVRamMode) == Graphics.LcdStatusFlags.ScanLineVRamMode)
                        return 0xFF;
                    return _device.Gpu.ReadVRam(address - 0x8000);
                case 0xA: // switchable ram (0xA000 -> 0xBFFF)
                case 0xB:
                    return _device.Cartridge.ReadByte(address);
                    
                case 0xC: // internal ram (0xC000 -> 0xCFFF)
                    return _internalRam[address - 0xC000];

                case 0xD: // internal switchable ram (0xD000 -> 0xDFFF)
                    return _internalSwitchableRam[address - 0xD000 + GetSwitchableRamOffset()];

                case 0xE: // Echo internal ram (0xE000 -> 0xEFFF)
                    return _internalRam[address - 0xE000];

                case 0xF:
                    switch (address & 0xFF00)
                    {
                        default: // Echo internal ram (0xF000 -> 0xFDFF)
                            return _internalSwitchableRam[address - 0xF000 + GetSwitchableRamOffset()];

                        case OAMLocation:
                            if (address < 0xFEA0) // OAM (0xFE00 -> 0xFE9F)
                            {   // Inaccessable in mode 2 & 3
                                if ((byte)(_device.Gpu.LCDMode & Graphics.LcdStatusFlags.ModeMask) > 1)
                                    return 0xFF;
                                return _device.Gpu.ReadOam((byte)(address & 0xFF));
                            }
                            else // Empty (0xFEA0 -> 0xFEFF)
                                return 0x0;
                        case 0xFF00: // IO (0xFF00 -> 0xFFFF)
                            switch (address & 0xFF)
                            {
                                case 0x00:
                                    return _device.KeyPad.JoyP;
                                case 0x01:
                                    return 0x80; // TODO: serial
                                case 0x02:
                                    return 0xFE; // TODO: serial
                                case 0x04:
                                case 0x05:
                                case 0x06:
                                case 0x07:
                                    return _device.Timer.ReadRegister((ushort) (address & 0xFF));
                                case 0x0F:
                                    return (byte) _device.Cpu.Registers.IF;
                                case 0x10:
                                case 0x11:
                                case 0x12:
                                case 0x13:
                                case 0x14:
                                case 0x15:
                                case 0x16:
                                case 0x17:
                                case 0x18:
                                case 0x19:
                                case 0x1A:
                                case 0x1B:
                                case 0x1C:
                                case 0x1D:
                                case 0x1E:
                                case 0x1F:
                                case 0x20:
                                case 0x21:
                                case 0x22:
                                case 0x23:
                                case 0x24:
                                case 0x25:
                                case 0x26:
                                    return _device.Spu.ReadRegister(address);
                                case 0x40:
                                case 0x41:
                                case 0x42:
                                case 0x43:
                                case 0x44:
                                case 0x45:
                                case 0x47:
                                case 0x48:
                                case 0x49:
                                case 0x4A:
                                case 0x4B:
                                case 0x4F:
                                case 0x68:
                                case 0x69:
                                case 0x6A:
                                case 0x6B:
                                    return _device.Gpu.ReadRegister((byte) (address & 0xFF));
                                case 0x4C:
                                    return _io[address - 0xFF49];
                                case 0x4D:
                                    return (byte) ((_device.Cpu.DoubleSpeed ? (1 << 7) : 0) |
                                                   (_device.Cpu.IsPreparingSpeedSwitch ? 1 : 0));
                                case 0x70:
                                    return (byte) _internalRamBankIndex;
                                default:
                                    if (address < 0xFF80)
                                        return 0;
                                    return _highInternalRam[address - 0xFF80];
                                case 0xFF:
                                    return (byte) _device.Cpu.Registers.IE;
                            }
                    }
            }
            
            throw new ArgumentOutOfRangeException(nameof(address), $"Memory address {address:X4} is not addressible.");
        }

        public byte[] ReadBytes(ushort address, int length)
        {
            byte[] result;
            if (length == 1)
                result = _singleOprandBuffer;
            else if (length == 2)
                result = _doubleOprandBuffer;
            else
                result = new byte[length];

            for (var i = 0; i < length; i++)
                result[i] = ReadByte((ushort)(address + i));

            return result;
        }

        public ushort ReadUInt16(ushort address)
        {
            return BitConverter.ToUInt16(ReadBytes(address, 2), 0);
        }

        public void WriteByte(ushort address, byte value, bool lockBus = true)
        {
            if (_device.Memory.RAMIsBusy && lockBus)
            {
                if (address >= HighRAMLocation && address != 0xFFFF)
                {
                    _highInternalRam[address - HighRAMLocation] = value;
                    return;
                }
                else
                {
                    return;
                }
            }
            switch (address >> 12)
            {
                case 0x0: // rom (0x0000 -> 0x3FFF)
                case 0x1:
                case 0x2:
                case 0x3:
                case 0x4: // switchable rom (0x4000 -> 0x7FFF)
                case 0x5:
                case 0x6:
                case 0x7:
                    _device.Cartridge.WriteByte(address, value);
                    break;

                case 0x8: // vram (0x8000 -> 0x9FFF)
                case 0x9:
                    if ((_device.Gpu.LCDMode & Graphics.LcdStatusFlags.ScanLineVRamMode) != Graphics.LcdStatusFlags.ScanLineVRamMode)
                        _device.Gpu.WriteVRam((ushort) (address - 0x8000), value);
                    break;

                case 0xA: // switchable ram (0xA000 -> 0xBFFF)
                case 0xB:
                    _device.Cartridge.WriteByte(address, value);
                    break;

                case 0xC: // internal ram (0xC000 -> 0xCFFF)
                    _internalRam[address - 0xC000] = value;
                    break;

                case 0xD: // internal switchable ram (0xD000 -> 0xDFFF)
                    _internalSwitchableRam[address - 0xD000 + GetSwitchableRamOffset()] = value;
                    break;

                case 0xE: // Echo internal ram (0xE000 -> 0xEFFF)
                    _internalRam[address - 0xE000] = value;
                    break;

                case 0xF:
                    switch (address & 0xFF00)
                    {
                        default: // Echo internal ram (0xF000 -> 0xFDFF)
                            _internalSwitchableRam[address - 0xF000 + GetSwitchableRamOffset()] = value;
                            break;

                        case 0xFE00:
                            if (address < 0xFEA0) // OAM (0xFE00 -> 0xFE9F)
                            {
                                // Inaccessable in mode 2 & 3
                                if ((byte)(_device.Gpu.LCDMode & Graphics.LcdStatusFlags.ModeMask) > 1)
                                    return;
                                _device.Gpu.WriteOam((byte)(address & 0xFF), value);
                            }
                            break;
                        case 0xFF00: // IO (0xFF00 -> 0xFFFF)
                            switch (address & 0xFF)
                            {
                                case 0x00:
                                    _device.KeyPad.JoyP = value;
                                    break;
                                case 0x04:
                                case 0x05:
                                case 0x06:
                                case 0x07:
                                    _device.Timer.WriteRegister((ushort) (address & 0xFF), value);
                                    break;
                                case 0x0F:
                                    _device.Cpu.Registers.IF = (InterruptFlags) (0xE0 | value);
                                    break;
                                case 0x10:
                                case 0x11:
                                case 0x12:
                                case 0x13:
                                case 0x14:
                                case 0x15:
                                case 0x16:
                                case 0x17:
                                case 0x18:
                                case 0x19:
                                case 0x1A:
                                case 0x1B:
                                case 0x1C:
                                case 0x1D:
                                case 0x1E:
                                case 0x1F:
                                case 0x20:
                                case 0x21:
                                case 0x22:
                                case 0x23:
                                case 0x24:
                                case 0x25:
                                case 0x26:
                                    _device.Spu.WriteRegister(address, value);
                                    break;
                                case 0x40:
                                case 0x41:
                                case 0x42:
                                case 0x43:
                                case 0x44:
                                case 0x45:
                                case 0x47:
                                case 0x48:
                                case 0x49:
                                case 0x4A:
                                case 0x4B:
                                case 0x4F:
                                case 0x68:
                                case 0x69:
                                case 0x6A:
                                case 0x6B:
                                    _device.Gpu.WriteRegister((byte)(address & 0xFF), value);
                                    break;
                                case 0x46:
                                case 0x51:
                                case 0x52:
                                case 0x53:
                                case 0x54:
                                case 0x55:
                                    DmaController.WriteRegister(address, value);
                                    break;
                                case 0x4D:
                                    _device.Cpu.IsPreparingSpeedSwitch = (value & 1) == 1; 
                                    break;
                                case 0x70:
                                    SwitchRamBank(value);
                                    break;
                                default:
                                    if (address >= 0xFF80)
                                        _highInternalRam[address - 0xFF80] = value;
                                    break;
                                case 0xFF:
                                    _device.Cpu.Registers.IE = (InterruptFlags) value;
                                    break;
                            }
                            break;
                    }
                    break;
            }
        }

        public void WriteBytes(ushort address, byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++) 
                WriteByte((ushort) (address + i), bytes[i]);
        }

        public void WriteUInt16(ushort address, ushort value)
        {
            WriteBytes(address, BitConverter.GetBytes(value));
        }

        private int GetSwitchableRamOffset() 
            => _device.GbcMode ? (_internalRamBankIndex - 1) * 0x1000 : 0;

        private void SwitchRamBank(byte value)
        {
            if (value == 0)
                value = 1;
            _internalRamBankIndex = value & 7;
        } 
    }
}
