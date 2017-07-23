namespace Emux.GameBoy.Graphics
{
    public interface IVideoOutput
    {
        void RenderFrame(byte[] pixelData);
    }

    public sealed class EmptyVideoOutput : IVideoOutput
    {
        public void RenderFrame(byte[] pixelData)
        {
        }
    }
}
