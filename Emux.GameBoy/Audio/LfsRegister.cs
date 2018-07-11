using System;

namespace Emux.GameBoy.Audio
{
    public class LfsRegister
    {
        private readonly ISoundChannel _channel;
        private short _state = 0x7F;

        public LfsRegister(ISoundChannel channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }
        
        public bool CurrentValue
        {
            get { return (_state & 1) == 1; }
        }

        public bool Use7BitStepWidth
        {
            get { return (_channel.NR3 & (1 << 3)) != 0; }
            set
            {
                _channel.NR3 = (byte) ((_channel.NR3 & ~(1 << 3)) | (value ? (1 << 3) : 0)); 
                Reset();
            }
        }

        public void Reset()
        {
            _state = (short) (Use7BitStepWidth ? 0x7F : 0x7FFF);
        }
        
        public void PerformShift()
        {
            byte nextBit = (byte) (((_state >> 1) & 1) ^ (_state & 1));
            
            if (Use7BitStepWidth)
            {
                _state >>= 1;
                _state |= (short) (nextBit << 6);
            }
            else
            {
                _state >>= 1;
                _state |= (short) (nextBit << 14);
            }
        }
    }
}