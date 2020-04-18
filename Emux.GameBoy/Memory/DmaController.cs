using System;
using Emux.GameBoy.Graphics;
using static Emux.GameBoy.Memory.GameBoyMemory;

namespace Emux.GameBoy.Memory
{
    public class DmaController : IGameBoyComponent
    {
        private const byte EnableMask = 0b10000000; // 0x80
        private const byte LengthMask = 0b01111111; // 0x7F

        private readonly GameBoy _device;
        private bool _isTransferring;
        private int _currentBlockIndex;
        private byte _sourceHigh;
        private byte _sourceLow;
        private byte _destinationHigh;
        private byte _destinationLow;
        private byte _dmaLengthMode;
        private readonly byte[] _block = new byte[OAMDMABlockSize];

        public DmaController(GameBoy device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            _device = device;
        }

        public ushort SourceAddress => (ushort)((_sourceHigh << 8) | _sourceLow & 0xF0);

        public ushort DestinationAddress => (ushort)(0x8000 | (((_destinationHigh << 8) | (_destinationLow & 0xF0)) & 0b0001111111110000));

        public bool DMAEnabled => (_dmaLengthMode & EnableMask) > 0;

        public int Length => ((_dmaLengthMode & LengthMask) + 1) * 0x10;

        public void Initialize()
        {
            _device.Gpu.HBlankStarted += GpuOnHBlankStarted;
        }

        public void Reset()
        {
            _isTransferring = false;
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
                    return _sourceHigh;
                case 0xFF52:
                    return _sourceLow;
                case 0xFF53:
                    return _destinationHigh;
                case 0xFF54:
                    return _destinationLow;
                case 0xFF55:
                    return _dmaLengthMode;
            }

            throw new ArgumentOutOfRangeException(nameof(address));
        }

        public void WriteRegister(ushort address, byte value)
        {
            switch (address)
            {
                case 0xFF46:
                    PerformOamDmaTransfer(value);
                    break;
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
                    if (_isTransferring && (value & EnableMask) == 0)
                    {
                        StopVramDmaTransfer();
                    }
                    else
                    {
                        _dmaLengthMode = value;
                        StartVramDmaTransfer();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(address));
            }
        }

        private void StopVramDmaTransfer()
        {
            _dmaLengthMode |= EnableMask;
            _currentBlockIndex = 0;
            _isTransferring = false;
        }

        private void StartVramDmaTransfer()
        {
            if (!DMAEnabled)
            {
                byte[] vram = new byte[Length];
                _device.Memory.ReadBlock(SourceAddress, vram, 0, vram.Length);
                _device.Gpu.WriteVRam((ushort) (DestinationAddress - VRAMStartAddress), vram, 0, vram.Length);
            }
            else
            {
                _currentBlockIndex = 0;
                _isTransferring = true;
                _dmaLengthMode &= LengthMask;
            }
        }

        private void PerformOamDmaTransfer(byte dma)
        {
            byte[] oamData = new byte[OAMSize];
            _device.Memory.ReadBlock((ushort) (dma * 0x100), oamData, 0, OAMSize);
            _device.Gpu.ImportOam(oamData);
        }

        private void GpuOnHBlankStarted(object sender, EventArgs eventArgs)
        {
            if (_isTransferring && _device.Gpu.LY < GameBoyGpu.FrameHeight)
                HDmaStep();
        }

        private void HDmaStep()
        {
            int currentOffset = _currentBlockIndex * OAMDMABlockSize;

            _device.Memory.ReadBlock((ushort)(SourceAddress + currentOffset), _block, 0, _block.Length);
            _device.Gpu.WriteVRam((ushort)(DestinationAddress - VRAMStartAddress + currentOffset), _block, 0, _block.Length);

            _currentBlockIndex++;
            int next = (_dmaLengthMode & LengthMask) - 1;
            _dmaLengthMode = (byte) ((_dmaLengthMode & EnableMask) | next);

            if (next <= 0)
            {
                _dmaLengthMode = 0xFF;
                _isTransferring = false;
            }
        }
    }
}
