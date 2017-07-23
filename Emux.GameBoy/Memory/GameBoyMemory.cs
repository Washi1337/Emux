using System;
using Emux.GameBoy.Cpu;
using Emux.GameBoy.Graphics;

namespace Emux.GameBoy.Memory
{
    public class GameBoyMemory
    {
        private readonly GameBoy _device;

        private readonly byte[] _rom = new byte[0x4000];
        private readonly byte[] _switchableRom = new byte[0x4000];
        private readonly byte[] _switchableRam = new byte[0x2000];
        private readonly byte[] _internalRam = new byte[0x2000];
        private readonly byte[] _highInternalRam = new byte[0x7F];
        
        // TODO: to be removed:
        private readonly byte[] _io = new byte[4];

        public GameBoyMemory(GameBoy device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            _device = device;

            Buffer.BlockCopy(device.Cartridge.RomContents, 0, _rom, 0, 0x4000);
            Buffer.BlockCopy(device.Cartridge.RomContents, 0x4000, _switchableRom, 0, 0x4000);

            DmaController = new DmaController(this);
        }

        public DmaController DmaController
        {
            get;
        }

        public byte ReadByte(ushort address)
        {
            switch (address >> 12)
            {
                case 0x0: // rom (0x0000 -> 0x3FFF)
                case 0x1:
                case 0x2:
                case 0x3:
                    return _rom[address];

                case 0x4: // switchable rom (0x4000 -> 0x7FFF)
                case 0x5:
                case 0x6:
                case 0x7:
                    return _switchableRom[address - 0x4000];

                case 0x8: // vram (0x8000 -> 0x9FFF)
                case 0x9:
                    return _device.Gpu.ReadVRam(address - 0x8000);

                case 0xA: // switchable ram (0xA000 -> 0xBFFF)
                case 0xB:
                    return _switchableRam[address - 0xA000];
                    
                case 0xC: // internal ram (0xC000 -> 0xDFFF)
                case 0xD:
                    return _internalRam[address - 0xC000];

                case 0xE: // Echo internal ram (0xE000 -> 0xEFFF)
                    return _internalRam[address - 0xE000];

                case 0xF:
                    switch (address & 0xFF00)
                    {
                        default: // Echo internal ram (0xF000 -> 0xFDFF)
                            return _internalRam[address - 0xE000];

                        case 0xFE00:
                            if (address < 0xFEA0) // OAM (0xFE00 -> 0xFE9F)
                                return _device.Gpu.ReadOam((byte) (address & 0xFF));
                            else // Empty (0xFEA0 -> 0xFEFF)
                                return 0x0;
                        case 0xFF00: // IO (0xFF00 -> 0xFFFF)
                            switch (address & 0xFF)
                            {
                                case 0x00:
                                    return _device.KeyPad.JoyP;
                                case 0x40:
                                case 0x41:
                                case 0x42:
                                case 0x43:
                                case 0x44:
                                case 0x45:
                                case 0x47:
                                case 0x48:
                                    return _device.Gpu.ReadRegister((byte) (address & 0xFF));
                                case 0x46:
                                    return DmaController.DmaTransfer;
                                case 0x49:
                                case 0x4A:
                                case 0x4B:
                                case 0x4C:
                                    return _io[address - 0xFF49];
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
            var result = new byte[length];
            for (int i = 0; i < length; i++)
                result[i] = ReadByte((ushort) (address + i));
            return result;
        }

        public ushort ReadUInt16(ushort address)
        {
            return BitConverter.ToUInt16(ReadBytes(address, 2), 0);
        }

        public void WriteByte(ushort address, byte value)
        {
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
                    return;
                    //throw new NotImplementedException("MBC not implemented yet.");

                case 0x8: // vram (0x8000 -> 0x9FFF)
                case 0x9:
                    _device.Gpu.WriteVRam((ushort) (address - 0x8000), value);
                    return;

                case 0xA: // switchable ram (0xA000 -> 0xBFFF)
                case 0xB:
                    _switchableRam[address - 0xA000] = value;
                    return;

                case 0xC: // internal ram (0xC000 -> 0xDFFF)
                case 0xD:
                    _internalRam[address - 0xC000] = value;
                    return;

                case 0xE: // Echo internal ram (0xE000 -> 0xEFFF)
                    _internalRam[address - 0xE000] = value;
                    return;

                case 0xF:
                    switch (address & 0xFF00)
                    {
                        default: // Echo internal ram (0xF000 -> 0xFDFF)
                            _internalRam[address - 0xE000] = value;
                            return;

                        case 0xFE00:
                            if (address < 0xFEA0) // OAM (0xFE00 -> 0xFE9F)
                                _device.Gpu.WriteOam((byte)(address & 0xFF), value);
                            return;
                        case 0xFF00: // IO (0xFF00 -> 0xFFFF)
                            switch (address & 0xFF)
                            {
                                case 0x00:
                                    _device.KeyPad.WriteJoyP(value);
                                    return;
                                case 0x40:
                                case 0x41:
                                case 0x42:
                                case 0x43:
                                case 0x44:
                                case 0x45:
                                case 0x47:
                                case 0x48:
                                     _device.Gpu.WriteRegister((byte)(address & 0xFF), value);
                                    return;
                                case 0x46:
                                    //for (int i = 0; i < 160; i++)
                                    //{
                                    //    byte x = ReadByte((ushort) (value << 8 + i));
                                    //    WriteByte((ushort) (0xFE00 + i), x);
                                    //}
                                    DmaController.DmaTransfer = value;
                                    return;
                                default:
                                    if (address >= 0xFF80)
                                        _highInternalRam[address - 0xFF80] = value;
                                    return;
                                case 0xFF:
                                    _device.Cpu.Registers.IE = (InterruptFlags) value;
                                    return;
                            }
                    }
            }
            throw new ArgumentOutOfRangeException(nameof(address), $"Memory address {address:X4} is not addressible.");
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

        internal void PerformDmaTransfer(byte dma)
        {
            byte[] section = null;
            switch (dma >> 4)
            {
                case 0x0: // rom (0x0000 -> 0x3FFF)
                case 0x1:
                case 0x2:
                case 0x3:
                    section = _rom;
                    break;
                case 0x4: // switchable rom (0x4000 -> 0x7FFF)
                case 0x5:
                case 0x6:
                case 0x7:
                    section = _switchableRom;
                    dma -= 0x40;
                    break;

                case 0x8: // vram (0x8000 -> 0x9FFF)
                case 0x9:
                    throw new NotImplementedException();

                case 0xA: // switchable ram (0xA000 -> 0xBFFF)
                case 0xB:
                    section = _switchableRam;
                    dma -= 0xA0;
                    break;

                case 0xC: // internal ram (0xC000 -> 0xDFFF)
                case 0xD:
                    section = _internalRam;
                    dma -= 0xC0;
                    break;

                case 0xE: // Echo internal ram (0xE000 -> 0xEFFF)
                    section = _internalRam;
                    dma -= 0xE0;
                    break;

                case 0xF:
                    section = _internalRam;
                    dma -= 0xE0;
                    break;
            }

            byte[] oamData = new byte[0xA0];
            Buffer.BlockCopy(section, dma*0x100, oamData, 0, oamData.Length);
            _device.Gpu.ImportOam(oamData);
        }
    }
}
