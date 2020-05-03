using System.IO;
using Emux.GameBoy.Graphics;
using SkiaSharp;

namespace Automation
{
    internal class FileVideoOutput : IVideoOutput
    {
        private byte[] _pixelData;
        private string _outputPath;

        public void SetSize(int width, int height)
        {

        }

        public FileVideoOutput(string outputPath)
        {
            _outputPath = outputPath;
        }

        public void RenderFrame(byte[] pixelData)
        {
            _pixelData = pixelData;
        }
        public void Blit()
        {
            var image = SKImage.Create(new SKImageInfo(GameBoyGpu.FrameWidth, GameBoyGpu.FrameHeight));
            var bitmap = SKBitmap.FromImage(image);

            int i = 0;
            for (var y=0; y<GameBoyGpu.FrameHeight; y++)
            {
                for (var x=0; x<GameBoyGpu.FrameWidth; x++)
                {
                    bitmap.SetPixel(x, y, new SKColor(_pixelData[i++], _pixelData[i++], _pixelData[i++]));
                }
            }

            image = SKImage.FromBitmap(bitmap);
            using var imageStream = image.Encode(SKEncodedImageFormat.Png, 100);
            using var fileStream = File.OpenWrite(_outputPath);
            imageStream.SaveTo(fileStream);
        }
    }
}