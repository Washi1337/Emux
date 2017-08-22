namespace Emux.GameBoy
{
    public interface IGameBoyComponent
    {
        void Initialize();

        void Reset();

        void Shutdown();
    }
}
