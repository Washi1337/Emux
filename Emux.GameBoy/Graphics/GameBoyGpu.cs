using System;
using System.Threading;
using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Graphics
{
    /// <summary>
    /// Represents the graphics processor unit of a GameBoy device.
    /// </summary>
    public unsafe class GameBoyGpu : IGameBoyComponent
    {
        public const int FrameWidth = 160;
        public const int FrameHeight = 144;
        
        public const int ScanLineOamCycles = 80;
        public const int ScanLineVramCycles = 172;
        public const int HBlankCycles = 204;
        public const int OneLineCycles = 456;
        public const int VBlankCycles = 456 * 10;
        public const int FullFrameCycles = 70224;

        public event EventHandler HBlankStarted;
        public event EventHandler VBlankStarted;

        private readonly byte[] _frameIndices = new byte[FrameWidth * FrameHeight];
        private readonly byte[] _frameBuffer = new byte[3 * FrameWidth * FrameHeight];
        private readonly GameBoy _device;
        private IVideoOutput _videoOutput;

        private readonly byte[] _vram;
        private readonly byte[] _oam = new byte[0xA0];
        private readonly byte[] _bgPaletteMemory = new byte[0x40];
        private readonly byte[] _spritePaletteMemory = new byte[0x40];

        private readonly Color[] _greyshades =
        {
            new Color(224, 248, 208),
            new Color(136, 192, 112),
            new Color(52, 104, 86),
            new Color(8, 24, 32),
        };

        private int _modeClock;
        private LcdControlFlags _lcdc;
        private byte _ly;
        private byte _lyc;
        
        public GameBoyGpu(GameBoy device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            _device = device;
            VideoOutput = new EmptyVideoOutput();
            Utilities.Memset(_bgPaletteMemory, 0xFF, _bgPaletteMemory.Length);
            _vram = new byte[device.GbcMode ? 0x4000 : 0x2000];
        }

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
                    Utilities.Memset(_frameBuffer, 255, _frameBuffer.Length);
                    Array.Clear(_frameIndices, 0, _frameIndices.Length);
                    VideoOutput.RenderFrame(_frameBuffer);
                    LY = 0;
                    SwitchMode(LcdStatusFlags.HBlankMode);
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

        public byte LY
        {
            get { return _ly; }
            set
            {
                if (_ly != value)
                {
                    _ly = value;
                    CheckCoincidenceInterrupt();
                }
            }
        }

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
        public IVideoOutput VideoOutput
        {
            get => _videoOutput;
            set {
                _videoOutput = value;
                _videoOutput.RenderFrame(_frameBuffer);
            }
        }

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
            Buffer.BlockCopy(buffer, offset, _vram, address + GetVRamOffset(), length);
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
                case 0x44:
                    LY = value;
                    return;
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
                    BgpD = value;
                    return;
                case 0x6A:
                    ObpI = value;
                    return;
                case 0x6B:
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
                    return BgpD;
                case 0x6A:
                    return ObpI;
                case 0x6B:
                    return ObpD;
            }

            throw new ArgumentOutOfRangeException(nameof(address));
        }
        
        public void Initialize()
        {
            _device.Cpu.PerformedStep += CpuOnPerformedStep;
        }

        public void Reset()
        {
            _modeClock = 0;
            LY = 0;
            ScY = 0;
            ScX = 0;
            Stat = (LcdStatusFlags)0x85;

            Lcdc = (LcdControlFlags) 0x91;
            ScY = 0;
            ScX = 0;
            _lyc = 0;
            Bgp = 0xFC;
            ObjP0 = 0xFF;
            ObjP1 = 0xFF;
            WY = 0;
            WX = 0;
            
            Utilities.Memset(_bgPaletteMemory, 0xFF, _bgPaletteMemory.Length);
            Utilities.Memset(_vram, 0, _vram.Length);
        }

        public void Shutdown()
        {
            _device.Cpu.PerformedStep -= CpuOnPerformedStep;
        }

        private void CpuOnPerformedStep(object sender, StepEventArgs args)
        {
            GpuStep(args.Cycles);
        }

        /// <summary>
        /// Advances the execution of the graphical processor unit.
        /// </summary>
        /// <param name="cycles">The cycles the central processor unit has executed since last step.</param>
        private void GpuStep(int cycles)
        {
            if ((_lcdc & LcdControlFlags.EnableLcd) == 0)
                return;

            unchecked
            {
                LcdStatusFlags stat = Stat;
                var currentMode = stat & LcdStatusFlags.ModeMask;
                _modeClock += cycles;

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
                            OnHBlankStarted();
                            RenderScan();
                        }
                        break;
                    case LcdStatusFlags.HBlankMode:
                        if (_modeClock >= HBlankCycles)
                        {
                            _modeClock -= HBlankCycles;
                            LY++;
                            if (LY == FrameHeight )
                            {
                                currentMode = LcdStatusFlags.VBlankMode;
                                OnVBlankStarted();
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
        
        private void CheckCoincidenceInterrupt()
        {
            if (LY == LYC && (Stat & LcdStatusFlags.CoincidenceInterrupt) != 0)
                _device.Cpu.Registers.IF |= InterruptFlags.LcdStat;
        }
        
        private void RenderScan()
        {
            if ((_lcdc & LcdControlFlags.EnableBackground) == LcdControlFlags.EnableBackground)
                RenderBackgroundScan();
            if ((_lcdc & LcdControlFlags.EnableWindow) == LcdControlFlags.EnableWindow)
                RenderWindowScan();
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
            int flippedTileDataOffset = 14 - tileDataOffset;

            int x = ScX;
            
            // Read first tile data to render.
            byte[] currentTileData = new byte[2];
            var flags = _device.GbcMode ? GetTileDataFlags(tileMapAddress, x >> 3 & 0x1F) : 0;
            CopyTileData(tileMapAddress, x >> 3 & 0x1F, tileDataAddress + ((flags & SpriteDataFlags.YFlip) != 0 ? flippedTileDataOffset : tileDataOffset), currentTileData, flags);

            // Render scan line.
            for (int outputX = 0; outputX < FrameWidth; outputX++, x++)
            {
                if ((x & 7) == 0)
                {
                    // Read next tile data to render.
                    if (_device.GbcMode)
                        flags = GetTileDataFlags(tileMapAddress, x >> 3 & 0x1F);
                    CopyTileData(tileMapAddress, x >> 3 & 0x1F, tileDataAddress + ((flags & SpriteDataFlags.YFlip) != 0 ? flippedTileDataOffset : tileDataOffset), currentTileData, flags);
                }
                
                RenderTileDataPixel(currentTileData, flags, outputX, x);
            }
        }

        private void RenderWindowScan()
        {
            if (LY >= WY)
            {
                // Move to correct tile map address.
                int tileMapAddress = (_lcdc & LcdControlFlags.WindowTileMapSelect)
                                     == LcdControlFlags.WindowTileMapSelect
                    ? 0x1C00
                    : 0x1800;

                int tileMapLine = ((LY - WY) & 0xFF) >> 3;
                tileMapAddress += tileMapLine * 0x20;

                // Move to correct tile data address.
                int tileDataAddress = (_lcdc & LcdControlFlags.BgWindowTileDataSelect) ==
                                      LcdControlFlags.BgWindowTileDataSelect
                    ? 0x0000
                    : 0x0800;

                int tileDataOffset = ((LY - WY) & 7) * 2;
                int flippedTileDataOffset = 14 - tileDataOffset;

                int x = 0;
                var flags = SpriteDataFlags.None;
                byte[] currentTileData = new byte[2];

                // Render scan line.
                for (int outputX = WX - 7; outputX < FrameWidth; outputX++, x++)
                {
                    if ((x & 7) == 0)
                    {
                        // Read next tile data to render.
                        if (_device.GbcMode)
                            flags = GetTileDataFlags(tileMapAddress, x >> 3 & 0x1F);
                        CopyTileData(tileMapAddress, x >> 3 & 0x1F, tileDataAddress + ((flags & SpriteDataFlags.YFlip) != 0 ? flippedTileDataOffset : tileDataOffset), currentTileData, flags);
                    }

                    if (outputX >= 0)
                        RenderTileDataPixel(currentTileData, flags, outputX, x);
                }
            }
        }
        
        private void RenderSpritesScan()
        {
            int spriteHeight = (Lcdc & LcdControlFlags.Sprite8By16Mode) != 0 ? 16 : 8;
            fixed (byte* ptr = _oam)
            {
                // GameBoy only supports 10 sprites in one scan line.
                int spritesCount = 0;
                for (int i = 0; i < 40 && spritesCount < 10; i++)
                {
                    var data = ((SpriteData*) ptr)[i];
                    int absoluteY = data.Y - 16;

                    // Check if sprite is on current scan line.
                    if (absoluteY <= LY && LY < absoluteY + spriteHeight)
                    {
                        // TODO: take order into account.
                        spritesCount++;

                        // Check if actually on the screen.
                        if (data.X > 0 && data.X < FrameWidth + 8)
                        {
                            // Read tile data.
                            int rowIndex = LY - absoluteY;

                            // Flip sprite vertically if specified.
                            if ((data.Flags & SpriteDataFlags.YFlip) == SpriteDataFlags.YFlip)
                                rowIndex = (spriteHeight - 1) - rowIndex;

                            // Read tile data.
                            int vramBankOffset = _device.GbcMode && (data.Flags & SpriteDataFlags.TileVramBank) != 0
                                ? 0x2000
                                : 0x0000;
                            byte[] currentTileData = new byte[2];
                            Buffer.BlockCopy(_vram, (ushort)(vramBankOffset + (data.TileDataIndex << 4) + rowIndex * 2), currentTileData, 0, 2);
                            
                            // Render sprite.
                            for (int x = 0; x < 8; x++)
                            {
                                int absoluteX = data.X - 8;

                                // Flip sprite horizontally if specified.
                                absoluteX += (data.Flags & SpriteDataFlags.XFlip) != SpriteDataFlags.XFlip ? x : 7 - x;

                                // Check if in frame and sprite is above or below background.
                                if (absoluteX >= 0 && absoluteX < FrameWidth 
                                    && ((data.Flags & SpriteDataFlags.BelowBackground) == 0 || GetRenderedColorIndex(absoluteX, LY) == 0))
                                {
                                    int colorIndex = GetPixelColorIndex(x, currentTileData);

                                    // Check if not transparent.
                                    if (colorIndex != 0)
                                    {
                                        if (_device.GbcMode)
                                        {
                                            int paletteIndex = (int)(data.Flags & SpriteDataFlags.PaletteNumberMask);
                                            RenderPixel(absoluteX, LY, colorIndex, GetGbcColor(_spritePaletteMemory, paletteIndex, colorIndex));
                                        }
                                        else
                                        {
                                            byte palette = (data.Flags & SpriteDataFlags.UsePalette1) == SpriteDataFlags.UsePalette1
                                                ? ObjP1
                                                : ObjP0;
                                            int greyshadeIndex = GetGreyshadeIndex(palette, colorIndex);
                                            RenderPixel(absoluteX, LY, colorIndex, _greyshades[greyshadeIndex]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CopyTileData(int tileMapAddress, int tileIndex, int tileDataAddress, byte[] buffer, SpriteDataFlags flags)
        {
            byte dataIndex = _vram[(ushort)(tileMapAddress + tileIndex)];
            if ((_lcdc & LcdControlFlags.BgWindowTileDataSelect) !=
                LcdControlFlags.BgWindowTileDataSelect)
            {
                // Index is signed number in [-128..127] => compensate for it.
                dataIndex = unchecked((byte)((sbyte)dataIndex + 0x80));
            }
            int bankOffset = ((flags & SpriteDataFlags.TileVramBank) != 0) ? 0x2000 : 0x0000;
            Buffer.BlockCopy(_vram, bankOffset + tileDataAddress + (dataIndex << 4), buffer, 0, 2);
        }
        
        private SpriteDataFlags GetTileDataFlags(int tileMapAddress, int tileIndex)
        {
           return (SpriteDataFlags) _vram[(ushort)(0x2000 + tileMapAddress + tileIndex)];
        }

        private static Color GetGbcColor(byte[] paletteMemory, int paletteIndex, int colorIndex)
        {
            ushort rawValue = (ushort)(paletteMemory[paletteIndex * 8 + colorIndex * 2] | (paletteMemory[paletteIndex * 8 + colorIndex * 2 + 1] << 8));
            return new Color(
                (byte)((rawValue & 0x1F) * (0xFF / 0x1F)),
                (byte)(((rawValue >> 5) & 0x1F) * (0xFF / 0x1F)),
                (byte)(((rawValue >> 10) & 0x1F) * (0xFF / 0x1F)));

        }
        
        private static int GetGreyshadeIndex(byte palette, int paletteIndex)
        {
            return (palette >> (paletteIndex * 2)) & 3;
        }

        private static int GetPixelColorIndex(int x, byte[] tileRowData)
        {
            int bitIndex = 7 - (x & 7);
            int paletteIndex = ((tileRowData[0] >> bitIndex) & 1) |
                               (((tileRowData[1] >> bitIndex) & 1) << 1);
            return paletteIndex;
        }
        
        private void RenderTileDataPixel(byte[] currentTileData, SpriteDataFlags flags, int outputX, int localX)
        {
            if (_device.GbcMode)
            {
                // TODO: support other flags.
                int actualX = localX & 7;

                // Horizontal flip when specified.
                if ((flags & SpriteDataFlags.XFlip) != 0)
                    actualX = 7 - actualX;
                
                if (LY == 0)
                {

                }
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

        private void RenderPixel(int x, int y, int colorIndex, Color color)
        {
            _frameIndices[y * FrameWidth + x] = (byte)colorIndex;
            fixed (byte* frameBuffer = _frameBuffer)
            {
                ((Color*)frameBuffer)[y * FrameWidth + x] = color;
            }
        }

        private int GetRenderedColorIndex(int x, int y)
        {
            return _frameIndices[y * FrameWidth + x];
        }
        
        private void SwitchMode(LcdStatusFlags mode)
        {
            Stat = (Stat & ~LcdStatusFlags.ModeMask) | mode;
        }

        private int GetVRamOffset()
        {
            return _device.GbcMode ? 0x2000 * Vbk : 0;
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
