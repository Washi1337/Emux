using System;

namespace Emux.GameBoy.Cpu
{
    public delegate void StepEventHandler(object sender, StepEventArgs args);

    public class StepEventArgs : EventArgs
    {
        public StepEventArgs(int cycles)
        {
            Cycles = cycles;
        }

        public int Cycles
        {
            get;
        }
    }
}
