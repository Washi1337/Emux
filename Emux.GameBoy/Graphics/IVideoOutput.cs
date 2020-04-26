using System;

namespace Emux.GameBoy.Graphics
{
    /// <summary>
    /// Represents a video output device.
    /// </summary>
    public interface IVideoOutput
    {
        /// <summary>
        /// Renders a frame to the output device.
        /// </summary>
        /// <param name="pixelData">The 24bit RGB pixel data that represents the 160x144 bitmap to render.</param>
        void RenderFrame(byte[] pixelData);
        void Blit();
    }

    /// <summary>
    /// Represents a virtual video output device that does not output anything.
    /// </summary>
    public sealed class EmptyVideoOutput : IVideoOutput
    {
        public void Blit()
        {
            
        }

        public void RenderFrame(byte[] pixelData)
        {

        }
    }
}
