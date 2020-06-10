using System;
using System.Collections.Generic;
using System.Text;

namespace Emux.GameBoy.Graphics
{
    public class FIFOPrefetcher
    {
        public struct PPUData
        {
            public byte[] tileData;
            public bool IsBackground;
            public byte SpriteIndex;
        }


        private SpriteData[] _sprites = new SpriteData[10];
        private GameBoy _device;
        private PPUData _output;
        private uint _cycle;
        private int _address;
        private byte _spriteIndex = 255;


        public int SpritesOnScanline { get; private set; }
        public bool DataIsReady { get; private set; }
        public bool PerformingSpriteFetch => _spriteIndex != 255;

        public FIFOPrefetcher()
        {
            _output = new PPUData()
            {
                tileData = new byte[2]
            };
        }

        public unsafe void OAMSearch(byte[] oam, int scanline, int spriteHeightPx)
        {
            byte foundSprites = 0;
            fixed (byte* ptr = oam)
            {
                for (var i=0; i<oam.Length/4; i++)
                {
                    var sprite = ((SpriteData*)ptr)[i];
                    var spriteX = sprite.X - 8;
                    var spriteY = sprite.Y - 16;
                    if (scanline < spriteY || scanline > spriteY + spriteHeightPx || spriteX == 0 || spriteX > GameBoyGpu.FrameWidth)
                        continue;
                    _sprites[foundSprites++] = sprite;
                }
            }

            SpritesOnScanline = foundSprites;
        }

        public void SetLocation(int address)
        {
            _address = address;
            _cycle = 0;
        }
        public void SpriteFetch(byte spriteIndex)
        {
            _spriteIndex = spriteIndex;
            _cycle = 0;
        }

        public void Step(uint cycles, byte[] vram)
        {
            do
            {
                step(vram);
                _cycle++;
            } while (--cycles > 0);
        }

        private void step(byte[] memory) // VRAM or OAM
        {
            if (PerformingSpriteFetch)
            {
                switch (_cycle)
                {
                    case 1:
                        //_location = _location.Slice(2);
                        break;
                    case 3:
                        _output.tileData[0] = memory[_address + _spriteIndex * 4];
                        break;
                    case 5:
                        _output.tileData[1] = memory[_address + _spriteIndex * 4 + 1];
                        _output.IsBackground = false;
                        _output.SpriteIndex = _spriteIndex;
                        DataIsReady = true;
                        break;
                }
            } 
            else
            {
                switch (_cycle)
                {
                    case 1:
                        //_location = _location.Slice(2);
                        break;
                    case 3:
                        _output.tileData[0] = memory[_address];
                        break;
                    case 5:
                        _output.tileData[1] = memory[_address + 1];
                        _output.IsBackground = true;
                        _output.SpriteIndex = 0;
                        DataIsReady = true;
                        break;
                }
            }
        }

        public PPUData Read()
        {
            _cycle = 0;
            if (PerformingSpriteFetch)
                _spriteIndex = 255;
            else
                _address += 2;

            return _output;
        }
    }
}
