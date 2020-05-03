using System;
using System.Collections.Generic;
using System.Text;

namespace Emux.GameBoy.Graphics
{
    public class DebugGameBoyGpu : GameBoyGpu
    {
        public DebugGameBoyGpu(GameBoy device) : base(device)
        {
        }

        protected override (int width, int height) getFrameBufferSize() => (OneLineCycles, FrameWidth + 10);

        protected override void onScanlineOAMSearchTick()
        {
            drawOAMdebugOverlay();
        }

        protected override void onScanlinePixelTransferTick()
        {
            base.onScanlinePixelTransferTick();
        }

        protected override void OnHBlankStarted()
        {
            
        }

        protected override void onScanlineVBlankTick()
        {
            drawVblankDebugOverlay();
        }

        protected override int RenderScan()
        {
            var delayedCycles = ScX % 8; // PPU discards some pixels so stalls
            if ((_lcdc & LcdControlFlags.EnableBackground) == LcdControlFlags.EnableBackground)
                RenderBackgroundScan();
            if ((_lcdc & LcdControlFlags.EnableWindow) == LcdControlFlags.EnableWindow)
                RenderWindowScan();
            if ((_lcdc & LcdControlFlags.EnableSprites) == LcdControlFlags.EnableSprites)
                RenderSpritesScan(ref delayedCycles);

            drawVramModesDebug(delayedCycles);

            return delayedCycles;
        }

        private unsafe void drawVramModesDebug(int delayedCycles)
        {
            var pixelTransferColor = new Color(169, 194, 132);
            var hBlankColor = new Color(166, 159, 196);
            if (!_device.Cpu.Halted)
            {
                pixelTransferColor.Darken(30);
                hBlankColor.Darken(30);
            }
            fixed (byte* frameBuffer = _frameBuffer)
            {
                var colorBuffer = (Color*)frameBuffer;
                for (var x = FrameWidth; x < FrameWidth + delayedCycles; x++)
                    colorBuffer[LY * OneLineCycles + x + ScanLineOamSearchCycles] = pixelTransferColor;
                for (var x = FrameWidth + delayedCycles; x < OneLineCycles - ScanLineOamSearchCycles; x++)
                    colorBuffer[LY * OneLineCycles + x + ScanLineOamSearchCycles] = hBlankColor;
            }
        }
        private unsafe void drawOAMdebugOverlay()
        {
            var oamSearchColor = new Color(239, 133, 133);
            if (!_device.Cpu.Halted)
                oamSearchColor.Darken(30);
            fixed (byte* frameBuffer = _frameBuffer)
            {
                var colorBuffer = (Color*)frameBuffer;
                for (var x = 0; x < ScanLineOamSearchCycles; x++)
                    colorBuffer[LY * OneLineCycles + x] = oamSearchColor;
            }
        }

        private unsafe void drawVblankDebugOverlay()
        {
            var vBlankColor = new Color(220, 178, 123);
            if (_device.Cpu.Halted)
                vBlankColor.Darken(30);

            fixed (byte* frameBuffer = _frameBuffer)
            {
                var colorBuffer = (Color*)frameBuffer;
                for (var x = 0; x < OneLineCycles; x++)
                    colorBuffer[LY * OneLineCycles + x] = vBlankColor;
            }
        }

        protected unsafe override void RenderPixel(int x, int y, int colorIndex, Color color)
        {
            if (!_device.Cpu.Halted)
                color.Darken(30);

            _frameIndices[y * FrameWidth + x] = (byte)colorIndex;
            fixed (byte* frameBuffer = _frameBuffer)
            {
                ((Color*)frameBuffer)[y * OneLineCycles + x + ScanLineOamSearchCycles] = color;
            }
        }
    }
}
