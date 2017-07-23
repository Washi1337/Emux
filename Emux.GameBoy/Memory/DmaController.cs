using System;

namespace Emux.GameBoy.Memory
{
    public class DmaController
    {
        public const int DmaCycles = 300; //671;
        
        private int _dmaCycles = -1;
        private byte _dmaTransfer;

        private readonly GameBoyMemory _memory;

        public DmaController(GameBoyMemory memory)
        {
            if (memory == null)
                throw new ArgumentNullException(nameof(memory));
            _memory = memory;
        }

        public byte DmaTransfer
        {
            get { return _dmaTransfer; }
            set
            {
                _dmaTransfer = value;
                _dmaCycles = DmaCycles;
            }
        }

        public void Update(int cycles)
        {
            if (_dmaCycles < 0)
                return;

            _dmaCycles -= cycles;

            if (_dmaCycles < 0)
            {
                _memory.PerformDmaTransfer(DmaTransfer);
            }
        }
    }
}
