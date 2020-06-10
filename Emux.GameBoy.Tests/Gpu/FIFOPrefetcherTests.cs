using Emux.GameBoy.Graphics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Emux.GameBoy.Tests.Gpu
{
    [TestClass]
    public class FIFOPrefetcherTest
    {
        [TestMethod]
        public void OAMSearch_FindsSingle()
        {
            var oam = new byte[4];
            constructSprite(oam, 0, 0, 10, 0, SpriteDataFlags.None);

            var fifoPrefetcher = new FIFOPrefetcher();
            fifoPrefetcher.OAMSearch(oam, 0, 8);

            Assert.AreEqual(1, fifoPrefetcher.SpritesOnScanline);
        }

        [TestMethod]
        public void OAMSearch_FindsMultiple()
        {
            var oam = new byte[8];
            constructSprite(oam, 0, 0, 10, 0, SpriteDataFlags.None);
            constructSprite(oam, 1, 0, 20, 0, SpriteDataFlags.None);

            var fifoPrefetcher = new FIFOPrefetcher();
            fifoPrefetcher.OAMSearch(oam, 0, 8);

            Assert.AreEqual(2, fifoPrefetcher.SpritesOnScanline);
        }

        [TestMethod]
        public void OAMSearch_IgnoresOffscreen_Y()
        {
            var oam = new byte[8];
            constructSprite(oam, 0, 0, 10, 0, SpriteDataFlags.None);
            constructSprite(oam, 1, 0, -16, 0, SpriteDataFlags.None);

            var fifoPrefetcher = new FIFOPrefetcher();
            fifoPrefetcher.OAMSearch(oam, 0, 8);

            Assert.AreEqual(1, fifoPrefetcher.SpritesOnScanline);
        }

        [TestMethod]
        public void OAMSearch_IgnoresOffscreen_Top()
        {
            var oam = new byte[8];
            constructSprite(oam, 0, 0, 10, 0, SpriteDataFlags.None);
            constructSprite(oam, 1, 0, -16, 0, SpriteDataFlags.None);

            var fifoPrefetcher = new FIFOPrefetcher();
            fifoPrefetcher.OAMSearch(oam, 0, 8);

            Assert.AreEqual(1, fifoPrefetcher.SpritesOnScanline);
        }

        [TestMethod]
        public void OAMSearch_IgnoresOffscreen_Bottom()
        {
            var oam = new byte[8];
            constructSprite(oam, 0, 0, 10, 0, SpriteDataFlags.None);
            constructSprite(oam, 1, GameBoyGpu.FrameHeight, 10, 0, SpriteDataFlags.None);

            var fifoPrefetcher = new FIFOPrefetcher();
            fifoPrefetcher.OAMSearch(oam, 0, 8);

            Assert.AreEqual(1, fifoPrefetcher.SpritesOnScanline);
        }

        [TestMethod]
        public void Step_6CyclesIsReady()
        {
            var fifoPrefetcher = new FIFOPrefetcher();
            fifoPrefetcher.Step(6, new byte[1000]);

            Assert.IsTrue(fifoPrefetcher.DataIsReady);
        }

        [TestMethod]
        public void Step_5CyclesIsNotReady()
        {
            var fifoPrefetcher = new FIFOPrefetcher();
            fifoPrefetcher.Step(5, new byte[1000]);

            Assert.IsFalse(fifoPrefetcher.DataIsReady);
        }

        [TestMethod]
        public void Step_BG_ReadsCorrectAddress()
        {
            var vram = new byte[1000];
            vram[100] = 1;
            vram[101] = 2;
            var fifoPrefetcher = new FIFOPrefetcher();
            fifoPrefetcher.SetLocation(100);
            fifoPrefetcher.Step(6, vram);

            var result = fifoPrefetcher.Read();
            Assert.AreEqual(1, result.tileData[0]);
            Assert.AreEqual(2, result.tileData[1]);
        }

        [TestMethod]
        public void Step_BG_Next_ReadsCorrectAddress()
        {
            var vram = new byte[1000];
            vram[100] = 1;
            vram[101] = 2;
            vram[102] = 3;
            vram[103] = 4;
            var fifoPrefetcher = new FIFOPrefetcher();
            fifoPrefetcher.SetLocation(100);
            fifoPrefetcher.Step(6, vram);
            fifoPrefetcher.Read();
            fifoPrefetcher.Step(6, vram);
            var result = fifoPrefetcher.Read();
            Assert.AreEqual(3, result.tileData[0]);
            Assert.AreEqual(4, result.tileData[1]);
        }

        [TestMethod]
        public void Step_BG_FlagsBGRead()
        {
            var vram = new byte[1000];
            vram[100] = 1;
            vram[101] = 2;
            var fifoPrefetcher = new FIFOPrefetcher();
            fifoPrefetcher.SetLocation(100);
            fifoPrefetcher.Step(6, vram);

            var result = fifoPrefetcher.Read();
            Assert.IsTrue(result.IsBackground);
        }

        [TestMethod]
        public void Step_Sprite_ReadsCorrectAddress()
        {
            byte spriteIndex = 5;
            var vram = new byte[1000];
            vram[spriteIndex * 4] = 1;
            vram[spriteIndex * 4 + 1] = 2;
            var fifoPrefetcher = new FIFOPrefetcher();
            fifoPrefetcher.SpriteFetch(spriteIndex);
            fifoPrefetcher.Step(6, vram);

            var result = fifoPrefetcher.Read();
            Assert.AreEqual(1, result.tileData[0]);
            Assert.AreEqual(2, result.tileData[1]);
        }

        [TestMethod]
        public void Step_Sprite_ContinuesBGFetch()
        {
            var vram = new byte[1000];
            vram[100] = 1;
            vram[101] = 2;
            var fifoPrefetcher = new FIFOPrefetcher();
            fifoPrefetcher.SetLocation(100);
            fifoPrefetcher.SpriteFetch(5);

            fifoPrefetcher.Step(6, vram);
            fifoPrefetcher.Read();
            fifoPrefetcher.Step(6, vram);
            var result = fifoPrefetcher.Read();

            Assert.AreEqual(1, result.tileData[0]);
            Assert.AreEqual(2, result.tileData[1]);
        }

        [TestMethod]
        public void Step_Sprite_FlagsSpriteRead()
        {
            byte spriteIndex = 5;
            var vram = new byte[1000];
            var fifoPrefetcher = new FIFOPrefetcher();
            fifoPrefetcher.SpriteFetch(spriteIndex);
            fifoPrefetcher.Step(6, vram);

            var result = fifoPrefetcher.Read();
            Assert.IsFalse(result.IsBackground);
        }

        [TestMethod]
        public void Step_Sprite_HasSpriteIndex()
        {
            byte spriteIndex = 5;
            var vram = new byte[1000];
            var fifoPrefetcher = new FIFOPrefetcher();
            fifoPrefetcher.SpriteFetch(spriteIndex);
            fifoPrefetcher.Step(6, vram);

            var result = fifoPrefetcher.Read();
            Assert.AreEqual(spriteIndex, result.SpriteIndex);
        }

        private void constructSprite(byte[] oam, byte index, int y, int x, byte TileDataIndex, SpriteDataFlags flags)
        {
            oam[index * 4 + 0] = (byte)(y + 16);
            oam[index * 4 + 1] = (byte)(x + 8);
            oam[index * 4 + 2] = TileDataIndex;
            oam[index * 4 + 3] = (byte)flags;
        }
    }
}
