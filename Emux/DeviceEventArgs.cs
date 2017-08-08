using System;

namespace Emux
{
    public class DeviceEventArgs : EventArgs
    {
        public DeviceEventArgs(GameBoy.GameBoy device)
        {
            Device = device;
        }

        public GameBoy.GameBoy Device
        {
            get;
        }
    }
}