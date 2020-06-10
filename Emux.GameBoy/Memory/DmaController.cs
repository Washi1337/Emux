using System;
using Emux.GameBoy.Graphics;
using static Emux.GameBoy.Memory.GameBoyMemory;

namespace Emux.GameBoy.Memory
{
    public class DmaController : IGameBoyComponent
    {
        public enum DMAType
        {
            None,
            OAM,
            HBlank,
            General
        }

        private const byte
            TransferingMask = 0b10000000, // 0x80
            VBlankMask = TransferingMask, // 0x80
            LengthMask = 0b01111111; // 0x7F

        private readonly GameBoy _device;
        private byte _sourceHigh;
        private byte _sourceLow;
        private byte _destinationHigh;
        private byte _destinationLow;
        private DMAType _activeDMA;
        private byte _OAMDMAIndex;
        private ushort _DMAIndex;
        private ushort _OAMDMAAddress;
        private int currentScanline, transferedThisScanline;

        public DmaController(GameBoy device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        public ushort SourceAddress => (ushort)((_sourceHigh << 8) | _sourceLow & 0xF0); // 0x0000 - 0x7FF0

        public ushort DestinationAddress => (ushort)(0x8000 | (((_destinationHigh << 8) | (_destinationLow & 0xF0)) & 0b0001111111110000)); // 0x8000 - 0x9FF0

        public int RemianingDMALength { get; private set; }

        public bool DMAIsActive => ActiveDMA != DMAType.None;

        private byte HDMA5 
        {
            set
            {
                RemianingDMALength = ((value & LengthMask) + 1) * 16;

                var isHblank = (value & TransferingMask) == TransferingMask;
                if (ActiveDMA == DMAType.HBlank && (value & TransferingMask) == 0)
                {
                    StopVramDmaTransfer();
                }
                else
                {
                    StartVramDmaTransfer(isHblank);
                }
            }
            get
            {
                var active = DMAIsActive ? 0 : TransferingMask;
                var length = RemianingDMALength / 16 - 1;
                return (byte)(active | length);
            } 
        }

        public DMAType ActiveDMA 
        {
            get => _activeDMA;
            private set 
            {
                _activeDMA = value;
                
                if (value == DMAType.OAM)
                    _device.Memory.RAMIsBusy = true;
                else if (value == DMAType.General)
                    _device.Memory.ROMIsBusy = true;
                else if (value == DMAType.None)
                {
                    _device.Memory.ROMIsBusy = false;
                    _device.Memory.RAMIsBusy = false;
                }
            }
        }

        public void Initialize()
        {
            _device.Gpu.HBlankTick += GpuHBlankTick;
        }

        public void Reset()
        {
            _activeDMA = DMAType.None;
            _sourceHigh = 0;
            _sourceLow = 0;
            _destinationHigh = 0;
            _destinationLow = 0;
            RemianingDMALength = 0;
        }

        public void Shutdown()
        {
            _device.Gpu.HBlankTick -= GpuHBlankTick;
        }

        internal void Step()
        {
            switch (ActiveDMA)
            {
                case DMAType.None:
                    return;
                case DMAType.OAM:
                    {
                        var value = _device.Memory.ReadByte((ushort)(_OAMDMAAddress + _OAMDMAIndex), false);
                        _device.Memory.WriteByte((ushort)(0xFE00 + _OAMDMAIndex), value, false);
                        _OAMDMAIndex++;
                        if (_OAMDMAIndex == OAMSize)
                            ActiveDMA = DMAType.None;
                    }
                    break;
                case DMAType.General:
                    {
                        var value = _device.Memory.ReadByte((ushort)(SourceAddress + _DMAIndex));
                        _device.Memory.WriteByte((ushort)(DestinationAddress + _DMAIndex), value);
                        _DMAIndex++;
                        RemianingDMALength--;

                        if (RemianingDMALength == 0 || DestinationAddress + _DMAIndex > ushort.MaxValue)
                        {
                            ActiveDMA = DMAType.None;
                            _device.Memory.ROMIsBusy = false;
                        }
                    }
                    break;
            }
        }

        public byte ReadRegister(ushort address)
        {
            switch (address)
            {
                case 0xFF46:
                    return 0;
                case 0xFF51:
                    return (_device.GbcMode) ? _sourceHigh : (byte)0xFF;
                case 0xFF52:
                    return (_device.GbcMode) ? _sourceLow : (byte)0xFF;
                case 0xFF53:
                    return (_device.GbcMode) ? _destinationHigh : (byte)0xFF;
                case 0xFF54:
                    return (_device.GbcMode) ? _destinationLow : (byte)0xFF;
                case 0xFF55:
                    return (_device.GbcMode) ? HDMA5 : (byte)0xFF;
            }

            throw new ArgumentOutOfRangeException(nameof(address));
        }

        public void WriteRegister(ushort address, byte value)
        {
            if (address == 0xFF46)
            {
                // Writing to this register launches a DMA transfer from ROM or RAM to OAM memory (sprite attribute table).
                // The written value specifies the transfer source address divided by 0x100
                // The transfer takes 160 machine cycles
                ActiveDMA = DMAType.OAM;
                _OAMDMAAddress = (ushort)(value * 0x100);
                _OAMDMAIndex = 0;
                return;
            }

            // GBC registers
            if (!_device.GbcMode)
                return;
            switch (address)
            {
                case 0xFF51:
                    _sourceHigh = value;
                    break;
                case 0xFF52:
                    _sourceLow = value;
                    break;
                case 0xFF53:
                    _destinationHigh = value;
                    break;
                case 0xFF54:
                    _destinationLow = value;
                    break;
                case 0xFF55:
                    HDMA5 = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(address));
            }
        }

        private void StopVramDmaTransfer()
        {
            // Once 0xFF is written to, bit 7 set indicates that it is not active
            ActiveDMA = DMAType.None;
        }

        private void StartVramDmaTransfer(bool isHBlank)
        {
            if (!isHBlank)
            {
                ActiveDMA = DMAType.General;
                _device.Memory.ROMIsBusy = true;
            }
            else
            {
                ActiveDMA = DMAType.HBlank;
                currentScanline = _device.Gpu.LY;
                transferedThisScanline = 0;
            }

            _DMAIndex = 0;
        }


        private void GpuHBlankTick(object sender, EventArgs e)
        {
            if (ActiveDMA == DMAType.HBlank && _device.Gpu.LY < GameBoyGpu.FrameHeight)
                NewHDmaStep();
        }

        private void NewHDmaStep() 
        {
            if (transferedThisScanline == 16)
            {
                if (_device.Gpu.LY == currentScanline)
                {
                    _device.Memory.ROMIsBusy = false;
                    return;
                }
                else
                {
                    currentScanline = _device.Gpu.LY;
                    transferedThisScanline = 0;
                    _device.Memory.ROMIsBusy = true;
                }
            }

            var value = _device.Memory.ReadByte((ushort)(SourceAddress + _DMAIndex));
            _device.Memory.WriteByte((ushort)(DestinationAddress + _DMAIndex), value);
            _DMAIndex++;
            RemianingDMALength--;
            transferedThisScanline++;

            if (RemianingDMALength == 0 || DestinationAddress + _DMAIndex > ushort.MaxValue)
                ActiveDMA = DMAType.None;
        }
    }
}
