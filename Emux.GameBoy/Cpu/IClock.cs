using System;

namespace Emux.GameBoy.Cpu
{
    public interface IClock
    {
        event EventHandler Tick;

        void Start();

        void Stop();
    }
}