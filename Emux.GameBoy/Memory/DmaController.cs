using System;

namespace Emux.GameBoy.Memory
{
    public class DmaController : IGameBoyComponent
    {
        private readonly GameBoy _device;
        private byte _sourceHigh;
        private byte _sourceLow;
        private byte _destinationHigh;
        private byte _destinationLow;
        private byte _dmaLengthMode;

        public DmaController(GameBoy device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            _device = device;
        }

        public ushort SourceAddress
        {
            get { return (ushort)((_sourceHigh << 8) | _sourceLow & 0xF0); }
        }

        public ushort DestinationAddress
        {
            get { return (ushort)(0x8000 | ((_destinationHigh & 0x1F) << 8) | _destinationLow & 0xF0); }
        }

        public int Length
        {
            get { return (_dmaLengthMode & 0x7F + 1) * 0x10; }
        }

        public void Initialize()
        {
            _device.Gpu.HBlankStarted += GpuOnHBlankStarted;
        }

        public void Reset()
        {
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
                    _dmaLengthMode = value;
                    StartVramDmaTransfer();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(address));
            }
        }

        private void StartVramDmaTransfer()
        {
            // TODO: distinguish between the two possible DMA transfer modes.
            byte[] vram = new byte[Length];
            _device.Memory.ReadBlock(SourceAddress, vram, 0, vram.Length);
            _device.Gpu.WriteVRam((ushort) (DestinationAddress - 0x8000), vram, 0, vram.Length);
        }

        private void PerformOamDmaTransfer(byte dma)
        {
            byte[] oamData = new byte[0xA0];
            _device.Memory.ReadBlock((ushort) (dma * 0x100), oamData, 0, oamData.Length);
            _device.Gpu.ImportOam(oamData);
        }

        private void GpuOnHBlankStarted(object sender, EventArgs eventArgs)
        {

        }
    }
}
