using System;
using System.Runtime.InteropServices;
using Emux.GameBoy;
using Emux.GameBoy.Cpu;

namespace Emux
{
    internal class WinMmTimer : IClock
    {
        public delegate void MmTimerProc(uint timerid, uint msg, IntPtr user, uint dw1, uint dw2);

        [DllImport("winmm.dll")]
        private static extern uint timeSetEvent(
            uint uDelay,
            uint uResolution,
            [MarshalAs(UnmanagedType.FunctionPtr)] MmTimerProc lpTimeProc,
            uint dwUser,
            int fuEvent
        );

        [DllImport("winmm.dll")]
        private static extern uint timeKillEvent(uint timerId);
        
        private readonly MmTimerProc _callback;
        private readonly int _frequency;
        private uint _timerId;

        public WinMmTimer(int frequency)
        {
            _callback = (timerid, msg, user, dw1, dw2) => Tick?.Invoke(null, EventArgs.Empty);
            _frequency = frequency;
        }

        public event EventHandler Tick;

        public void Start()
        {
            _timerId = timeSetEvent((uint) (1000 / _frequency), 0, _callback, 0, 1);
        }

        public void Stop()
        {
            timeKillEvent(_timerId);
        }
    }
}
