using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Emux.GameBoy.Cheating
{
    public class GamesharkController
    {
        private GameBoy _device;

        public GamesharkController()
        {
            Codes = new ObservableCollection<GamesharkCode>();
        }

        public GameBoy Device
        {
            get { return _device; }
            set
            {
                if (_device != value)
                {
                    if (_device != null)
                        _device.Gpu.VBlankStarted -= GpuOnVBlankStarted;
                    _device = value;
                    if (value != null)
                        value.Gpu.VBlankStarted += GpuOnVBlankStarted;
                }
            }
        }

        public ObservableCollection<GamesharkCode> Codes
        {
            get;
        }
        
        private void GpuOnVBlankStarted(object sender, EventArgs e)
        {
            lock (Codes)
            {
                foreach (var code in Codes)
                {
                    if (code.Enabled)
                        _device.Memory.WriteByte(code.Address, code.Value);
                }
            }
        }
    }
}
