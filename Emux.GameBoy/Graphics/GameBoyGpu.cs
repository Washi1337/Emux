﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Graphics
{
    /// <summary>
    /// Represents the graphics processor unit of a GameBoy device.
    /// </summary>
    public unsafe class GameBoyGpu : IGameBoyComponent
    {
        public const int FrameWidth = 160,
            FrameHeight = 144,
            ScanLineOamSearchCycles = 80,
            ScanLineMode3MinCycles = 168,
            ScanLineMode3MaxCycles = 291,
            ScanLineMode0MinCycles = 85,
            ScanLineMode0MaxCycles = 208,
            OneLineCycles = ScanLineOamSearchCycles + ScanLineMode3MinCycles + ScanLineMode0MaxCycles,
            VBlankCycles = OneLineCycles * 10,
            FullFrameCycles = 70224;

        protected const byte _pixelSizeBytes = 3;

        public event EventHandler HBlankStarted;
        public event EventHandler VBlankStarted;

        protected readonly GameBoy _device;

        protected readonly byte[] _vram;
        protected readonly byte[] _colorIndices = new byte[FrameWidth * FrameHeight];
        protected readonly byte[] _frameBuffer;
        protected readonly byte[] _oam = new byte[0xA0];
        protected readonly byte[] _bgPaletteMemory = new byte[0x40];
        protected readonly byte[] _spritePaletteMemory = new byte[0x40];
        private readonly byte[] _spriteIndexes;

        protected readonly Color[] _greyshades =
        {
            new Color(224, 248, 208),
            new Color(136, 192, 112),
            new Color(52, 104, 86),
            new Color(8, 24, 32),
        };
        protected Color BGOffColor = new Color(255, 255, 255);

        protected int _frameClock = 4;
        private int _currentPixel = 0; // Seperate counter for Mode3 because its simpler
        protected LcdControlFlags _lcdc;
        protected byte _lyc;
        protected int _totalStalledPixels, _currentlyStalledPixels;
        
        public GameBoyGpu(GameBoy device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            VideoOutput = new EmptyVideoOutput();
            _bgPaletteMemory.AsSpan().Fill(0xFF);
            _vram = new byte[device.GbcMode ? 0x4000 : 0x2000];

            var (width, height) = getFrameBufferSize();
            _frameBuffer = new byte[width * height * _pixelSizeBytes];
            _spriteIndexes = new byte[FrameWidth];
        }

        protected virtual (int width, int height) getFrameBufferSize() => (FrameWidth, FrameHeight);


        public Color Color0
        {
            get { return _greyshades[0]; }
            set { _greyshades[0] = value; }
        }

        public Color Color1
        {
            get { return _greyshades[1]; }
            set { _greyshades[1] = value; }
        }

        public Color Color2
        {
            get { return _greyshades[2]; }
            set { _greyshades[2] = value; }
        }

        public Color Color3
        {
            get { return _greyshades[3]; }
            set { _greyshades[3] = value; }
        }

        public LcdControlFlags Lcdc
        {
            get { return _lcdc; }
            set
            {
                if ((value & LcdControlFlags.EnableLcd) == 0)
                {
                    ClearBuffer();
                    VideoOutput.RenderFrame(_frameBuffer);
                    SwitchMode(LcdStatusFlags.HBlankMode);
                    _frameClock = 0;
                }
                else if ((_lcdc & LcdControlFlags.EnableLcd) == 0)
                {
                    if (LY == LYC)
                        Stat |= LcdStatusFlags.Coincidence;
                }

                _lcdc = value;
            }
        }

        public LcdStatusFlags LCDMode => Stat & LcdStatusFlags.ModeMask;


        public LcdStatusFlags Stat
        {
            get;
            set;
        }

        public byte ScY
        {
            get;
            set;
        }

        public byte ScX
        {
            get;
            set;
        }

        public byte LY => (byte)(_frameClock / OneLineCycles);

        public byte LYC
        {
            get { return _lyc; }
            set
            {
                if (_lyc != value)
                {
                    _lyc = value;
                    CheckCoincidenceInterrupt();
                }
            }
        }

        public byte Bgp
        {
            get;
            set;
        }

        public byte ObjP0
        {
            get;
            set;
        }

        public byte ObjP1
        {
            get;
            set;
        }

        public byte WY
        {
            get;
            set;
        }

        public byte WX
        {
            get;
            set;
        }

        public byte BgpI
        {
            get;
            set;
        }

        public byte BgpD
        {
            get { return _bgPaletteMemory[BgpI & 0x3f]; }
            set
            {
                _bgPaletteMemory[BgpI & 0x3f] = value;
                if ((BgpI & 0x80) != 0)
                    BgpI = (byte)(0x80 | ((BgpI + 1) & 0x3F));
            }
        }

        public byte ObpI
        {
            get;
            set;
        }

        public byte ObpD
        {
            get { return _spritePaletteMemory[ObpI & 0x3f]; }
            set
            {
                _spritePaletteMemory[ObpI & 0x3f] = value;
                if ((ObpI & 0x80) != 0)
                    ObpI++;
            }
        }

        public byte Vbk
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the output device the graphics processor should render frames to.
        /// </summary>
        private IVideoOutput _videoOutput;

        public IVideoOutput VideoOutput
        {
            get => _videoOutput;
            set
            {
                _videoOutput = value;
                var (width, height) = getFrameBufferSize();
                _videoOutput.SetSize(width, height);
            }
        }

        public bool IsMode(LcdStatusFlags mode) => (LCDMode & LcdStatusFlags.ModeMask) == mode;

        /// <summary>
        /// Writes a byte to the Object Attribute Memory (OAM).
        /// </summary>
        /// <param name="address">The address in the OAM to write the data to.</param>
        /// <param name="value">The value to write.</param>
        public void WriteOam(byte address, byte value)
        {
            _oam[address] = value;
        }

        /// <summary>
        /// Writes the given data to the Object Attribute Memory (OAM).
        /// </summary>
        /// <param name="oamData">The data to import.</param>
        public void ImportOam(byte[] oamData)
        {
            Buffer.BlockCopy(oamData, 0, _oam, 0, oamData.Length);
        }

        /// <summary>
        /// Reads a byte from the Object Attribute Memory (OAM).
        /// </summary>
        /// <param name="address">The address in the OAM to read the data from.</param>
        /// <returns></returns>
        public byte ReadOam(byte address)
        {
            return _oam[address];
        }

        /// <summary>
        /// Writes a byte to the Video RAM.
        /// </summary>
        /// <param name="address">The address in the VRAM to write the data to.</param>
        /// <param name="value">The value to write.</param>
        public void WriteVRam(ushort address, byte value)
        {
            _vram[address + GetVRamOffset()] = value;
        }

        public void WriteVRam(ushort address, byte[] buffer, int offset, int length)
        {
            var vramStartAddr = address + GetVRamOffset();
#if DEBUG
                if (offset + length > buffer.Length)
                    Console.WriteLine($"Buffer is too small. Expected {offset + length} elements but got {buffer.Length}.");
                if (vramStartAddr + length > _vram.Length)
                    Console.WriteLine($"Writing to out of range of VRAM. End address is {vramStartAddr + length} but end is at {_vram.Length}.");
#endif
            if (vramStartAddr + length > _vram.Length)
                length = _vram.Length - vramStartAddr;
            Buffer.BlockCopy(buffer, offset, _vram, vramStartAddr, length);
        }

        /// <summary>
        /// Reads a byte from the Video RAM.
        /// </summary>
        /// <param name="address">The address in the VRAM to write the data to.</param>
        /// <returns></returns>
        public byte ReadVRam(int address)
        {
            return _vram[address + GetVRamOffset()];
        }

        /// <summary>
        /// Assigns a new value to a graphics processor register identified by its relative IO memory address.
        /// </summary>
        /// <param name="address">The memory address relative to the I/O memory section (0xFF00).</param>
        /// <param name="value">The new value.</param>
        public void WriteRegister(byte address, byte value)
        {
            switch (address)
            {
                case 0x40:
                    Lcdc = (LcdControlFlags) value;
                    return;
                case 0x41:
                    Stat = (LcdStatusFlags) ((byte)Stat & 0b111 | value & 0b01111000);
                    return;
                case 0x42:
                    ScY = value;
                    return;
                case 0x43:
                    ScX = value;
                    return;
                // Cannot write to 0xFF44
                case 0x45:
                    LYC = value;
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
                case 0x4A:
                    WY = value;
                    return;
                case 0x4B:
                    WX = value;
                    return;
                case 0x4F:
                    Vbk = (byte) (value & 1);
                    return;
                case 0x68:
                    BgpI = value;
                    return;
                case 0x69:
                    if (!IsMode(LcdStatusFlags.ScanLineVRamMode))
                        BgpD = value;
                    return;
                case 0x6A:
                    if (!IsMode(LcdStatusFlags.ScanLineVRamMode))
                        ObpI = value;
                    return;
                case 0x6B:
                    if (!IsMode(LcdStatusFlags.ScanLineVRamMode))
                        ObpD = value;
                    return;
            }

            throw new ArgumentOutOfRangeException(nameof(address));
        }

        /// <summary>
        /// Reads the value of a graphics processor register identified by its relative IO memory address.
        /// </summary>
        /// <param name="address">The memory address relative to the I/O memory section (0xFF00).</param>
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
                case 0x47:
                    return Bgp;
                case 0x48:
                    return ObjP0;
                case 0x49:
                    return ObjP1;
                case 0x4A:
                    return WY;
                case 0x4B:
                    return WX;
                case 0x4F:
                    return Vbk;
                case 0x68:
                    return BgpI;
                case 0x69:
                    return IsMode(LcdStatusFlags.ScanLineVRamMode) ? (byte)0xFF : BgpD;
                case 0x6A:
                    return IsMode(LcdStatusFlags.ScanLineVRamMode) ? (byte)0xFF : ObpI;
                case 0x6B:
                    return IsMode(LcdStatusFlags.ScanLineVRamMode) ? (byte)0xFF : ObpD;
            }

            throw new ArgumentOutOfRangeException(nameof(address));
        }
        
        public void Initialize()
        {
        }

        public void Reset()
        {
            ScY = 0;
            ScX = 0;
            Stat = LcdStatusFlags.Coincidence | LcdStatusFlags.VBlankMode | (LcdStatusFlags)0b10000000;

            Lcdc = LcdControlFlags.EnableBackground | LcdControlFlags.BgWindowTileDataSelect | LcdControlFlags.EnableLcd;
            ScY = 0;
            ScX = 0;
            LYC = 0;
            Bgp = 0xFC;
            ObjP0 = 0xFF;
            ObjP1 = 0xFF;
            WY = 0;
            WX = 0;
        }

        private void ClearBuffer()
        {
            _bgPaletteMemory.AsSpan().Fill(0xff);
            _vram.AsSpan().Clear();
            _frameBuffer.AsSpan().Clear();
        }

        public void Shutdown()
        {
        }

        /// <summary>
        /// Advances the execution of the graphical processor unit.
        /// </summary>
        /// <param name="cycles">The cycles the central processor unit has executed since last step.</param>
        public void Step(int cycles)
        {
            for (var i = 0; i < cycles; i++)
            {
                Step();
                _frameClock++;
            }
        }

        private void Step()
        {
            if (_currentlyStalledPixels < _totalStalledPixels)
            {
                _currentlyStalledPixels++;
                return;
            }

            if ((_lcdc & LcdControlFlags.EnableLcd) == 0)
                return;

            var stat = Stat;
            var currentMode = stat & LcdStatusFlags.ModeMask;

            var scanline = LY;
            var scanlineDot = _frameClock % OneLineCycles;

            if (scanlineDot <= 4) // Temp hack. Not stepping one cycle at a time yet so could be out. 
                CheckCoincidenceInterrupt();

            if (scanline < FrameHeight)
            {
                if (scanlineDot <= ScanLineOamSearchCycles) // OAM Search
                {
                    if (scanlineDot == ScanLineOamSearchCycles - 1)
                        OAMSearch();
                    currentMode = LcdStatusFlags.ScanLineOamMode;
                }
                else if (currentMode == LcdStatusFlags.HBlankMode)
                {
                    OnScanlineHBlankTick();
                }
                else // Pixel transfer
                {
                    if (scanlineDot == ScanLineOamSearchCycles + 1)
                    {
                        _totalStalledPixels = 8; // 8 cycle 'stall' to fetch first background tile
                        _totalStalledPixels += ScX % 8; // PPU discards some pixels so stalls
                        return;
                    }
                    if (scanlineDot > ScanLineOamSearchCycles + _totalStalledPixels) 
                    {
                        currentMode = LcdStatusFlags.ScanLineVRamMode;

                        OnScanlinePixelTransferTick();

                        if (_currentPixel >= FrameWidth)
                        {
                            OnHBlankStarted();

                            currentMode = LcdStatusFlags.HBlankMode;

                            _currentlyStalledPixels = 0;
                            _currentPixel = 0;

                            if ((stat & LcdStatusFlags.HBlankModeInterrupt) == LcdStatusFlags.HBlankModeInterrupt)
                                _device.Cpu.Registers.IF |= InterruptFlags.LcdStat;
                        }
                        else
                        {
                            RenderPixel(_currentPixel++);
                        }
                    }
                }
            }
            else // In V-Blank
            {
                currentMode = LcdStatusFlags.VBlankMode;

                OnScanlineVBlankTick();

                if (scanlineDot == 0)
                {
                    if (scanline > FrameHeight + 9)
                    {
                        VideoOutput.RenderFrame(_frameBuffer);

                        _frameClock = 0;

                        if ((stat & LcdStatusFlags.OamBlankModeInterrupt) == LcdStatusFlags.OamBlankModeInterrupt)
                            _device.Cpu.Registers.IF |= InterruptFlags.LcdStat;
                    }
                    else if (scanline == FrameHeight)
                    {
                        OnVBlankStarted();

                        _device.Cpu.Registers.IF |= InterruptFlags.VBlank;
                        if ((stat & LcdStatusFlags.VBlankModeInterrupt) == LcdStatusFlags.VBlankModeInterrupt)
                            _device.Cpu.Registers.IF |= InterruptFlags.LcdStat;
                    }
                }
            }

            stat &= ~(LcdStatusFlags.ModeMask | LcdStatusFlags.Coincidence);
            stat |= currentMode;
            if (scanline == _lyc)
                stat |= LcdStatusFlags.Coincidence;
            Stat = stat;
        }

        protected virtual void OAMSearch()
        {
            fixed (byte* ptr = _oam)
            {
                var spriteHeight = (Lcdc & LcdControlFlags.Sprite8By16Mode) != 0 ? 16 : 8;
                var sprites = (SpriteData*)ptr;
                var spritesOnLine = Enumerable.
                    Range(0, 40)
                    .Select(i => (index: (byte)i, sprite: sprites[i]))
                    .Where(data => data.sprite.Y - 16 <= LY && LY < data.sprite.Y - 16 + spriteHeight && data.sprite.X > 0 && data.sprite.X <= FrameWidth + 8)
                    .OrderBy(data => data.index) // If two sprites are at the same X, the one earliest in OAM wins
                    .ThenBy(data => data.sprite.X)
                    .Take(10);
                var count = spritesOnLine.Count();

                _spriteIndexes.AsSpan().Fill(0xFF);
                foreach (var spriteData in spritesOnLine)
                {
                    for (int x = 0, spriteX = spriteData.sprite.X - 8; x < 8; x++, spriteX++)
                    {
                        if (spriteX >= 0 && spriteX < FrameWidth)
                            _spriteIndexes[spriteX] = spriteData.index;
                    }
                }
            }
        }

        protected virtual void RenderPixel(int pixel)
        {
            if ((_lcdc & LcdControlFlags.EnableBackground) == LcdControlFlags.EnableBackground)
                RenderBackground(pixel);
            else
                RenderPixel(pixel, LY, 0, BGOffColor);
            if ((_lcdc & LcdControlFlags.EnableWindow) == LcdControlFlags.EnableWindow)
                RenderWindow(pixel);
            if ((_lcdc & LcdControlFlags.EnableSprites) == LcdControlFlags.EnableSprites)
                RenderSprite(pixel);
        }

        protected void RenderBackground(int pixel)
        {
            // Move to correct tile map address.
            var tileMapAddress = (_lcdc & LcdControlFlags.BgTileMapSelect) == LcdControlFlags.BgTileMapSelect
                ? 0x1C00
                : 0x1800;

            var tileMapLine = ((LY + ScY) & 0xFF) >> 3;
            tileMapAddress += tileMapLine * 0x20;

            // Move to correct tile data address.
            var tileDataAddress = (_lcdc & LcdControlFlags.BgWindowTileDataSelect) ==
                                  LcdControlFlags.BgWindowTileDataSelect
                ? 0x0000
                : 0x0800;

            var tileDataOffset = ((LY + ScY) & 7) * 2;
            var flippedTileDataOffset = 14 - tileDataOffset;

            var startPixel = pixel;
            var x = ScX + startPixel;

            // Read first tile data to render.
            var flags = _device.GbcMode ? GetTileDataFlags(tileMapAddress, x / 8 & 31) : 0;
            
            // Read next tile data to render.
            if (_device.GbcMode)
                flags = GetTileDataFlags(tileMapAddress, x / 8 & 31);
            var tileData = GetTileData(
                tileMapAddress,
                x / 8 & 31,
                tileDataAddress + ((flags & SpriteDataFlags.YFlip) != 0 ? flippedTileDataOffset : tileDataOffset),
                flags
            );

            var outputX = startPixel;
            RenderTileDataPixel(tileData, flags, outputX, x);
        }
        
        protected void RenderSprite(int currentPixel)
        {
            var currentSpriteIndex = _spriteIndexes[currentPixel];
            if (currentSpriteIndex == 0xFF)
                return;
            SpriteData currentSprite;
            fixed (byte* ptr = _oam)
            {
                var sprites = (SpriteData*)ptr;
                currentSprite = sprites[currentSpriteIndex];
            }
            // Check if we just started drawing a new sprite
            if (currentPixel == 0 || _spriteIndexes[currentPixel - 1] != currentSpriteIndex)
                _totalStalledPixels += 11 - Math.Min(5, (currentSprite.X + ScX) % 8); // Simulate a PPU stall

            var startX = currentPixel - (currentSprite.X - 8);

            var spriteHeight = (Lcdc & LcdControlFlags.Sprite8By16Mode) != 0 ? 16 : 8;
            RenderSprite(spriteHeight, currentSprite, startX);
        }
        
        protected void RenderWindow(int currentPixel)
        {
            if (LY < WY)
                return;
            var actualX = WX - 7;
            if (actualX > 166)
                return;
            if (WY >= FrameHeight)
                return;
            if (currentPixel < actualX)
                return;
            if (currentPixel == WX)
                _totalStalledPixels += 6; // At least 6 cycles, exact number not known

            // Move to correct tile map address.
            var tileMapAddress = (_lcdc & LcdControlFlags.WindowTileMapSelect)
                                    == LcdControlFlags.WindowTileMapSelect
                ? 0x1C00
                : 0x1800;

            var tileMapLine = ((LY - WY) & 0xFF) >> 3;
            tileMapAddress += tileMapLine * 0x20;

            // Move to correct tile data address.
            var tileDataAddress = (_lcdc & LcdControlFlags.BgWindowTileDataSelect) ==
                                    LcdControlFlags.BgWindowTileDataSelect
                ? 0x0000
                : 0x0800;

            var tileDataOffset = ((LY - WY) & 7) * 2;
            var flippedTileDataOffset = 14 - tileDataOffset;

            var x = currentPixel - actualX;
            var flags = SpriteDataFlags.None;
            // Read next tile data to render.
            if (_device.GbcMode)
                flags = GetTileDataFlags(tileMapAddress, x >> 3 & 0x1F);
            var tileData = GetTileData(
                tileMapAddress, 
                x >> 3 & 0x1F,
                tileDataAddress + ((flags & SpriteDataFlags.YFlip) != 0 ? flippedTileDataOffset : tileDataOffset),
                flags
            );

            RenderTileDataPixel(tileData, flags, currentPixel, x);
        }

        private void RenderSprite(int spriteHeight, SpriteData sprite, int spriteColumn)
        {
            var absoluteY = sprite.Y - 16;
            var rowIndex = LY - absoluteY;

            // Flip sprite vertically if specified.
            if ((sprite.Flags & SpriteDataFlags.YFlip) == SpriteDataFlags.YFlip)
                rowIndex = (spriteHeight - 1) - rowIndex;

            // Read tile data.
            var vramBankOffset = _device.GbcMode && (sprite.Flags & SpriteDataFlags.TileVramBank) != 0
                ? 0x2000
                : 0x0000;
            var currentTileData = _vram.AsSpan((ushort)(vramBankOffset + (sprite.TileDataIndex << 4) + rowIndex * 2));

            // Render sprite.
            var screenX = sprite.X - 8 + spriteColumn;

            // Check if is above or below background.
            if ((sprite.Flags & SpriteDataFlags.BelowBackground) == 0 || GetRenderedColorIndex(screenX, LY) == 0)
            {
                // Flip sprite horizontally if specified.
                var colorIndex = GetPixelColorIndex(
                    (sprite.Flags & SpriteDataFlags.XFlip) != SpriteDataFlags.XFlip ? spriteColumn : 7 - spriteColumn,
                    currentTileData
                );

                // Check if not transparent.
                if (colorIndex == 0)
                    return;
                    
                if (_device.GbcMode)
                {
                    var paletteIndex = (int)(sprite.Flags & SpriteDataFlags.PaletteNumberMask);
                    RenderPixel(screenX, LY, colorIndex, GetGbcColor(_spritePaletteMemory, paletteIndex, colorIndex));
                }
                else
                {
                    var palette = (sprite.Flags & SpriteDataFlags.UsePalette1) == SpriteDataFlags.UsePalette1
                        ? ObjP1
                        : ObjP0;
                    var greyshadeIndex = GetGreyshadeIndex(palette, colorIndex);
                    RenderPixel(screenX, LY, colorIndex, _greyshades[greyshadeIndex]);
                }
            }
        }

        protected void CopyTileData(int tileMapAddress, int tileIndex, int tileDataAddress, byte[] buffer, SpriteDataFlags flags)
        {
            var dataIndex = _vram[tileMapAddress + tileIndex];
            if ((_lcdc & LcdControlFlags.BgWindowTileDataSelect) != LcdControlFlags.BgWindowTileDataSelect)
            {
                // Index is signed number in [-128..127] => compensate for it.
                dataIndex = unchecked((byte)((sbyte)dataIndex + 0x80));
            }
            var bankOffset = ((flags & SpriteDataFlags.TileVramBank) != 0) ? 0x2000 : 0x0000;
            Buffer.BlockCopy(_vram, bankOffset + tileDataAddress + (dataIndex << 4), buffer, 0, 2);
        }
        protected Span<byte> GetTileData(int tileMapAddress, int tileIndex, int tileDataAddress, SpriteDataFlags flags)
        {
            var dataIndex = _vram[tileMapAddress + tileIndex];
            if ((_lcdc & LcdControlFlags.BgWindowTileDataSelect) != LcdControlFlags.BgWindowTileDataSelect)
            {
                // Index is signed number in [-128..127] => compensate for it.
                dataIndex = unchecked((byte)((sbyte)dataIndex + 0x80));
            }
            var bankOffset = ((flags & SpriteDataFlags.TileVramBank) != 0) ? 0x2000 : 0x0000;
            return _vram.AsSpan(bankOffset + tileDataAddress + (dataIndex << 4));
        }

        protected SpriteDataFlags GetTileDataFlags(int tileMapAddress, int tileIndex)
        {
           return (SpriteDataFlags) _vram[(ushort)(0x2000 + tileMapAddress + tileIndex)];
        }

        protected static Color GetGbcColor(byte[] paletteMemory, int paletteIndex, int colorIndex)
        {
            ushort rawValue = (ushort)(paletteMemory[paletteIndex * 8 + colorIndex * 2] | (paletteMemory[paletteIndex * 8 + colorIndex * 2 + 1] << 8));
			return new Color(
				(byte)((rawValue & 0x1F) * (0xFF / 0x1F)),
				(byte)(((rawValue >> 5) & 0x1F) * (0xFF / 0x1F)),
				(byte)(((rawValue >> 10) & 0x1F) * (0xFF / 0x1F)));
		}

        protected static int GetGreyshadeIndex(byte palette, int paletteIndex)
        {
            return (palette >> (paletteIndex * 2)) & 3;
        }

        protected static int GetPixelColorIndex(int x, byte[] tileRowData)
        {
            int bitIndex = 7 - (x & 7);
            int paletteIndex = ((tileRowData[0] >> bitIndex) & 1) |
                               (((tileRowData[1] >> bitIndex) & 1) << 1);
            return paletteIndex;
        }
        protected static int GetPixelColorIndex(int x, Span<byte> tileRowData)
        {
            int bitIndex = 7 - (x & 7);
            int paletteIndex = ((tileRowData[0] >> bitIndex) & 1) |
                               (((tileRowData[1] >> bitIndex) & 1) << 1);
            return paletteIndex;
        }

        protected void RenderTileDataPixel(Span<byte> currentTileData, SpriteDataFlags flags, int outputX, int localX)
        {
            if (_device.GbcMode)
            {
                // TODO: support other flags.
                int actualX = localX & 7;

                // Horizontal flip when specified.
                if ((flags & SpriteDataFlags.XFlip) != 0)
                    actualX = 7 - actualX;
                
                int paletteIndex = (int)(flags & SpriteDataFlags.PaletteNumberMask);
                int colorIndex = GetPixelColorIndex(actualX, currentTileData);
                RenderPixel(outputX, LY, colorIndex, GetGbcColor(_bgPaletteMemory, paletteIndex, colorIndex));
            }
            else
            {
                int colorIndex = GetPixelColorIndex(localX & 7, currentTileData);
                int greyshadeIndex = GetGreyshadeIndex(Bgp, colorIndex);
                RenderPixel(outputX, LY, colorIndex, _greyshades[greyshadeIndex]);
            }
        }

        protected virtual void RenderPixel(int x, int y, int colorIndex, Color color)
        {
            _colorIndices[y * FrameWidth + x] = (byte)colorIndex;
            fixed (byte* frameBuffer = _frameBuffer)
            {
                ((Color*)frameBuffer)[y * FrameWidth + x] = color;
            }
        }

        protected int GetRenderedColorIndex(int x, int y)
        {
            return _colorIndices[y * FrameWidth + x];
        }

        protected void SwitchMode(LcdStatusFlags mode)
        {
            Stat = (Stat & ~LcdStatusFlags.ModeMask) | mode;
        }

        protected int GetVRamOffset()
        {
            return _device.GbcMode ? 0x2000 * Vbk : 0;
        }

        protected virtual void OnScanlinePixelTransferTick()
        {

        }

        protected virtual void OnScanlineHBlankTick()
        {

        }

        protected virtual void OnScanlineVBlankTick()
        {

        }

        private void CheckCoincidenceInterrupt()
        {
            if (LY == _lyc && (Stat & LcdStatusFlags.CoincidenceInterrupt) != 0)
                _device.Cpu.Registers.IF |= InterruptFlags.LcdStat;
        }

        protected virtual void OnHBlankStarted()
        {
            HBlankStarted?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnVBlankStarted()
        {
            VBlankStarted?.Invoke(this, EventArgs.Empty);
        }
    }
}
