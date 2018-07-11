using System;

namespace Emux.GameBoy.Audio
{
    /// <summary>
    /// Represents either a 7 bit or a 15 bit linear-feedback shift register used in the GameBoy.
    /// The taps that the GameBoy uses are 0 and 1.  
    /// </summary>
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
            _state >>= 1;
            _state |= (short) (nextBit << (Use7BitStepWidth ? 6 : 14));
        }
    }
}