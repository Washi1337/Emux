using System;
using System.Threading;
using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Graphics
{
    public unsafe class GameBoyGpu
    {
        public const int FrameWidth = 160;
        public const int FrameHeight = 144;
        
        public const int ScanLineOamCycles = 80;
        public const int ScanLineVramCycles = 172;
        public const int HBlankCycles = 204;
        public const int OneLineCycles = 456;
        public const int VBlankCycles = 456 * 10;
        public const int FullFrameCycles = 70224;
        
        private readonly byte[] _frameBuffer = new byte[3 * FrameWidth * FrameHeight];
        private readonly GameBoy _device;

        private readonly byte[] _vram = new byte[0x2000];
        private readonly byte[] _oam = new byte[0xA0];

        private readonly Color[] _colors =
        {
            new Color(255,255,255),
            new Color(192,192,192),
            new Color(96,96,96),
            new Color(0,0,0),
        };

        private int _modeClock;
        private LcdControlFlags _lcdc;

        public LcdControlFlags Lcdc
        {
            get { return _lcdc; }
            set
            {
                if ((value & LcdControlFlags.EnableLcd) == 0)
                {
                    Array.Clear(_frameBuffer, 0, _frameBuffer.Length);
                    VideoOutput.RenderFrame(_frameBuffer);
                    LY = 0;
                    Stat = (LcdStatusFlags) 0x80;
                    _modeClock = 0;
                }
                else if ((_lcdc & LcdControlFlags.EnableLcd) == 0)
                {
                    _modeClock = 0;
                    if (LY == LYC)
                        Stat |= LcdStatusFlags.Coincidence;
                }

                _lcdc = value;
            }
        }

        public LcdStatusFlags Stat;
        public byte ScY;
        public byte ScX;
        public byte LY;
        public byte LYC;
        public byte Dma;
        public byte Bgp;
        public byte ObjP0;
        public byte ObjP1;

        public GameBoyGpu(GameBoy device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            _device = device;
            VideoOutput = new EmptyVideoOutput();
        }
        
        public IVideoOutput VideoOutput
        {
            get;
            set;
        }

        public void WriteOam(byte address, byte value)
        {
            _oam[address] = value;
        }

        public void ImportOam(byte[] oamData)
        {
            Buffer.BlockCopy(oamData, 0, _oam, 0, oamData.Length);
        }

        public byte ReadOam(byte address)
        {
            return _oam[address];
        }

        public void WriteVRam(ushort address, byte value)
        {
            _vram[address] = value;
        }

        public byte ReadVRam(int address)
        {
            return _vram[address];
        }

        public void WriteRegister(byte address, byte value)
        {
            switch (address)
            {
                case 0x40:
                    Lcdc = (LcdControlFlags) value;
                    return;
                case 0x41:
                    Stat = (LcdStatusFlags) (value & 0b01111000);
                    return;
                case 0x42:
                    ScY = value;
                    return;
                case 0x43:
                    ScX = value;
                    return;
                case 0x44:
                    LY = value;
                    return;
                case 0x45:
                    LYC = value;
                    return;
                case 0x46:
                    Dma = value;
                    return;
                case 0x47:
                    Bgp = value;
                    return;
                case 0x48:
                    ObjP0 = value;
                    return;
                case 0x49:
                    ObjP1 = value;
                    return;
            }

            throw new ArgumentOutOfRangeException(nameof(address));
        }

        public byte ReadRegister(byte address)
        {
            switch (address)
            {
                case 0x40:
                    return (byte) _lcdc;
                case 0x41:
                    return (byte) Stat;
                case 0x42:
                    return ScY;
                case 0x43:
                    return ScX;
                case 0x44:
                    return LY;
                case 0x45:
                    return LYC;
                case 0x46:
                    return Dma;
                case 0x47:
                    return Bgp;
                case 0x48:
                    return ObjP0;
                case 0x49:
                    return ObjP1;
            }

            throw new ArgumentOutOfRangeException(nameof(address));
        }

        private void GpuStep()
        {
            if ((_lcdc & LcdControlFlags.EnableLcd) == 0)
                return;

            unchecked
            {
                LcdStatusFlags stat = Stat;
                
                var currentMode = stat & LcdStatusFlags.ModeMask;
                var lastMode = currentMode;
                switch (currentMode)
                {
                    case LcdStatusFlags.ScanLineOamMode:
                        if (_modeClock >= ScanLineOamCycles)
                        {
                            _modeClock -= ScanLineOamCycles;
                            currentMode = LcdStatusFlags.ScanLineVRamMode;
                        }
                        break;
                    case LcdStatusFlags.ScanLineVRamMode:
                        if (_modeClock >= ScanLineVramCycles)
                        {
                            _modeClock -= ScanLineVramCycles;
                            currentMode = LcdStatusFlags.HBlankMode;
                            if ((stat & LcdStatusFlags.HBlankModeInterrupt) == LcdStatusFlags.HBlankModeInterrupt)
                                _device.Cpu.Registers.IF |= InterruptFlags.LcdStat;
                            RenderScan();
                        }
                        break;
                    case LcdStatusFlags.HBlankMode:
                        if (_modeClock >= HBlankCycles)
                        {
                            _modeClock -= HBlankCycles;
                            LY++;
                            if (LY == FrameHeight)
                            {
                                currentMode = LcdStatusFlags.VBlankMode;
                                VideoOutput.RenderFrame(_frameBuffer);
                                _device.Cpu.Registers.IF |= InterruptFlags.VBlank;
                                if ((stat & LcdStatusFlags.VBlankModeInterrupt) == LcdStatusFlags.VBlankModeInterrupt)
                                    _device.Cpu.Registers.IF |= InterruptFlags.LcdStat;
                            }
                            else
                            {
                                currentMode = LcdStatusFlags.ScanLineOamMode;
                            }
                        }
                        break;
                    case LcdStatusFlags.VBlankMode:
                        if (_modeClock >= OneLineCycles)
                        {
                            _modeClock -= OneLineCycles;
                            LY++;

                            if (LY > FrameHeight + 9)
                            {
                                currentMode = LcdStatusFlags.ScanLineOamMode;
                                LY = 0;
                                if ((stat & LcdStatusFlags.OamBlankModeInterrupt) == LcdStatusFlags.OamBlankModeInterrupt)
                                    _device.Cpu.Registers.IF |= InterruptFlags.LcdStat;
                            }
                        }
                        break;
                }

                stat &= (LcdStatusFlags) ~0b111;
                stat |= currentMode;
                if (LY == LYC)
                    stat |= LcdStatusFlags.Coincidence;
                Stat = stat;
            }
        }
        
        private void RenderScan()
        {
            if ((_lcdc & LcdControlFlags.EnableBgAndWindow) == LcdControlFlags.EnableBgAndWindow)
                RenderBackgroundScan();
            if ((_lcdc & LcdControlFlags.EnableSprites) == LcdControlFlags.EnableSprites)
                RenderSpritesScan();
        }

        private void RenderBackgroundScan()
        {
            // Move to correct tile map address.
            int tileMapAddress = (_lcdc & LcdControlFlags.BgTileMapSelect) == LcdControlFlags.BgTileMapSelect
                ? 0x1C00
                : 0x1800;

            int tileMapLine = ((LY + ScY) & 0xFF) >> 3;
            tileMapAddress += tileMapLine * 0x20;

            // Move to correct tile data address.
            int tileDataAddress = (_lcdc & LcdControlFlags.BgWindowTileDataSelect) ==
                                  LcdControlFlags.BgWindowTileDataSelect
                ? 0x0000
                : 0x0800;

            int tileDataOffset = ((LY + ScY) & 7) * 2;
            tileDataAddress += tileDataOffset;

            int x = ScX;
            
            // REad first tile data to render.
            byte[] currentTileData = new byte[2];
            CopyTileData(tileMapAddress, x >> 3, tileDataAddress, currentTileData);

            // Render scan line.
            for (int outputX = 0; outputX < FrameWidth; outputX++, x++)
            {
                if ((x & 7) == 0)
                {
                    // Read next tile data to render..
                    CopyTileData(tileMapAddress, x >> 3, tileDataAddress, currentTileData);
                }

                var color = DeterminePixelColor(x, currentTileData, Bgp);
                _frameBuffer[LY * FrameWidth * 3 + outputX * 3] = color.R;
                _frameBuffer[LY * FrameWidth * 3 + outputX * 3 + 1] = color.G;
                _frameBuffer[LY * FrameWidth * 3 + outputX * 3 + 2] = color.B;
            }
        }

        private void CopyTileData(int tileMapAddress, int tileIndex, int tileDataAddress, byte[] buffer)
        {
            byte dataIndex = _vram[(ushort) (tileMapAddress + tileIndex)];
            Buffer.BlockCopy(_vram, tileDataAddress + (dataIndex << 4), buffer, 0, 2);
        }

        private void RenderSpritesScan()
        {
            fixed (byte* ptr = _oam)
            {
                // GameBoy only supports 10 sprites in one scan line.
                int spritesCount = 0;
                for (int i = 0; i < 40 && spritesCount < 10; i++)
                {
                    var data = ((SpriteData*) ptr)[i];
                    int absoluteY = data.Y - 16;

                    // Check if sprite is on current scan line.
                    if (absoluteY <= LY && LY < absoluteY + 8)
                    {
                        // TODO: take order into account.
                        spritesCount++;

                        // Check if actually on the screen.
                        if (data.X > 0 && data.X < FrameWidth + 8)
                        {
                            byte palette = (data.Flags & SpriteDataFlags.UsePalette1) == SpriteDataFlags.UsePalette1
                                ? ObjP1
                                : ObjP0;
                            
                            // Read tile data.
                            int rowIndex = LY - absoluteY;
                            byte[] currentTileData = new byte[2];
                            Buffer.BlockCopy(_vram, (ushort)(0x0000 + (data.TileDataIndex << 4) + rowIndex * 2),
                                currentTileData, 0, 2);

                            // Render sprite.
                            for (int j = 0; j < 8; j++)
                            {
                                int absoluteX = data.X - 8 + j;
                                if (absoluteX >= 0 && absoluteX < FrameWidth)
                                {
                                    int colorIndex = DetermineColorIndex(j, currentTileData, palette);
                                    
                                    // TODO: take priority into account.
                                    // TODO: take flip into account.

                                    // Check for transparent color.
                                    if (colorIndex != 0)
                                    {
                                        var color = _colors[colorIndex];
                                        _frameBuffer[LY * FrameWidth * 3 + absoluteX * 3] = color.R;
                                        _frameBuffer[LY * FrameWidth * 3 + absoluteX * 3 + 1] = color.G;
                                        _frameBuffer[LY * FrameWidth * 3 + absoluteX * 3 + 2] = color.B;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private Color DeterminePixelColor(int x, byte[] tileRowData, byte palette)
        {
            return _colors[DetermineColorIndex(x, tileRowData, palette)];
        }

        private static int DetermineColorIndex(int x, byte[] tileRowData, byte palette)
        {
            int bitIndex = 7 - (x & 7);
            int paletteIndex = (((tileRowData[0] >> bitIndex) & 1) << 1) |
                               ((tileRowData[1] >> bitIndex) & 1);
            int colorIndex = (palette >> (paletteIndex * 2)) & 3;
            return colorIndex;
        }

        internal void Update(int cycles)
        {
            _modeClock += cycles;
            GpuStep();
        }

        public void Reset()
        {
            _modeClock = 0;
            LY = 0;
            ScY = 0;
            ScX = 0;
            Stat = (LcdStatusFlags) 0x85;
        }

    }
}
