using System;
using Emux.GameBoy.Graphics;
using static Emux.GameBoy.Memory.GameBoyMemory;

namespace Emux.GameBoy.Memory
{
    public class DmaController : IGameBoyComponent
    {
        private const byte TransferingMask = 0b10000000; // 0x80
        private const byte VBlankMask = TransferingMask; // 0x80
        private const byte LengthMask = 0b01111111; // 0x7F

        private readonly GameBoy _device;
        private int _currentBlockIndex;
        private byte _sourceHigh;
        private byte _sourceLow;
        private byte _destinationHigh;
        private byte _destinationLow;
        private byte _dmaLengthMode;
        private bool _HBlankDMAactive;
        private readonly byte[] HDmaBlockCopy = new byte[OAMDMABlockSize];
        private readonly byte[] _OamBlockCopy = new byte[OAMSize];
        private readonly byte[] _vramBlockCopy = new byte[((LengthMask & LengthMask) + 1) * 0x10]; // Largest possible size

        public DmaController(GameBoy device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        public ushort SourceAddress => (ushort)((_sourceHigh << 8) | _sourceLow & 0xF0);

        public ushort DestinationAddress => (ushort)(0x8000 | (((_destinationHigh << 8) | (_destinationLow & 0xF0)) & 0b0001111111110000));

        public int Length => ((_dmaLengthMode & LengthMask) + 1) * 0x10;

        public void Initialize()
        {
            _device.Gpu.HBlankStarted += GpuOnHBlankStarted;
        }

        public void Reset()
        {
            _HBlankDMAactive = false;
            _currentBlockIndex = 0;
            _sourceHigh = 0;
            _sourceLow = 0;
            _destinationHigh = 0;
            _destinationLow = 0;
            _dmaLengthMode = 0;
        }

        public void Shutdown()
        {
            _device.Gpu.HBlankStarted -= GpuOnHBlankStarted;
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
                    return (_device.GbcMode) ? _dmaLengthMode : (byte)0xFF;
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
                PerformOamDmaTransfer(value);
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
                    var isHblank = (value >> 7) == 1;
                    if (_HBlankDMAactive && (value & TransferingMask) == 0)
                    {
                        StopVramDmaTransfer();
                    }
                    else
                    {
                        _dmaLengthMode = value;
                        StartVramDmaTransfer(isHblank);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(address));
            }
        }

        private void StopVramDmaTransfer()
        {
            // Once 0xFF is written to, bit 7 set indicates that it is not active
            _dmaLengthMode |= TransferingMask;
            _currentBlockIndex = 0;
            _HBlankDMAactive = false;
        }

        private void StartVramDmaTransfer(bool isHBlank)
        {
            // Once 0xFF is written to, bit 7 set indicates that it is not active
            _dmaLengthMode &= LengthMask;

            if (!isHBlank)
            {
                _device.Memory.ReadBlock(SourceAddress, _vramBlockCopy, 0, Length);
                _device.Gpu.WriteVRam((ushort)(DestinationAddress - VRAMStartAddress), _vramBlockCopy, 0, Length);
            }
            else
            {
                _HBlankDMAactive = true;
                _currentBlockIndex = 0;
            }
        }

        private void PerformOamDmaTransfer(byte dma)
        {
            _device.Memory.ReadBlock((ushort)(dma * 0x100), _OamBlockCopy, 0, OAMSize);
            _device.Gpu.ImportOam(_OamBlockCopy);
        }

        private void GpuOnHBlankStarted(object sender, EventArgs eventArgs)
        {
            if (_HBlankDMAactive && _device.Gpu.LY < GameBoyGpu.FrameHeight)
                HDmaStep();
        }

        private void HDmaStep()
        {
            var currentOffset = _currentBlockIndex * OAMDMABlockSize;

            _device.Memory.ReadBlock((ushort)(SourceAddress + currentOffset), HDmaBlockCopy, 0, OAMDMABlockSize);
            _device.Gpu.WriteVRam((ushort)(DestinationAddress - VRAMStartAddress + currentOffset), HDmaBlockCopy, 0, OAMDMABlockSize);

            _currentBlockIndex++;
            var next = (_dmaLengthMode & LengthMask) - 1;
            _dmaLengthMode = (byte) ((_dmaLengthMode & TransferingMask) | next);

            if (next <= 0)
            {
                _dmaLengthMode = 0xFF;
                _HBlankDMAactive = false;
            }
        }
    }
}
