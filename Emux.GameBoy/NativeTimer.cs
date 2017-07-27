using System;
using System.Runtime.InteropServices;

namespace Emux.GameBoy
{
    internal class NativeTimer
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

        public NativeTimer(MmTimerProc callback, int frequency)
        {
            _callback = callback;
            _frequency = frequency;
        }

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
