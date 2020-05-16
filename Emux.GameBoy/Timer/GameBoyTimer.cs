using System;
using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Timer
{
    public class GameBoyTimer : IGameBoyComponent
    {
        private readonly GameBoy _device;
        public const int DivFrequency = 16384;
        public const int DivCycleInterval = (int) (GameBoyCpu.OfficialClockFrequency / DivFrequency);

        private int _divClock;
        private int _timerClock;
        private TimerControlFlags _tac;
        private byte _tima;
        private byte _div;
        
        public byte Div
        {
            get { return _div; }
            set
            {
                _div = value;
                _timerClock = 0;
                _divClock = 0;
            }
        }

        public byte Tima
        {
            get { return _tima; }
            set { _tima = value; }
        }

        public byte Tma
        {
            get;
            set;
        }
        
        public TimerControlFlags Tac
        {
            get { return _tac; }
            set
            {
                if ((value & TimerControlFlags.EnableTimer) == 0)
                {
                    //_timerClock = 0;
                }

                _tac = value;
            }
        }

        public GameBoyTimer(GameBoy device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            _device = device;
        }

        public void Initialize()
        {
        }

        public void Reset()
        {
            Div = 0x1E;
            Tima = 0;
            Tma = 0;
            Tac = 0;
        }

        public void Shutdown()
        {
        }

        public int GetTimaFrequency()
        {
            switch (Tac & TimerControlFlags.ClockMask)
            {
                case TimerControlFlags.Clock4096Hz:
                    return 4096;
                case TimerControlFlags.Clock16384Hz:
                    return 16384;
                case TimerControlFlags.Clock65536Hz:
                    return 65536;
                case TimerControlFlags.Clock262144Hz:
                    return 262144;
            }
            return 0;
        }

        private int GetTimaClockCycles()
        {
            return (int)(GameBoyCpu.OfficialClockFrequency / GetTimaFrequency());
        }
		

        public void Step(int cycles)
        {
            _divClock += cycles;
            while (_divClock > DivCycleInterval)
            {
                _divClock -= DivCycleInterval;
                _div = (byte) ((Div + 1) % 0xFF);
            }
            
            if ((_tac & TimerControlFlags.EnableTimer) == TimerControlFlags.EnableTimer)
            {
                _timerClock += cycles;
                int timaCycles = GetTimaClockCycles();
                while (_timerClock > timaCycles)
                {
                    _timerClock -= timaCycles;

                    int result = _tima + 1;
                    _tima = (byte) (result & 0xFF);
                    if (result > 0xFF)
                    {
                        _tima = Tma;
                        _device.Cpu.Registers.IF |= InterruptFlags.Timer;
                    }
                }
            }
        }

        public byte ReadRegister(ushort address)
        {
            switch (address)
            {
                case 0x04:
                    return Div;
                case 0x05:
                    return Tima;
                case 0x06:
                    return Tma;
                case 0x07:
                    return (byte) Tac;
            }
            throw new ArgumentOutOfRangeException(nameof(address));
        }

        public void WriteRegister(ushort address, byte value)
        {
            switch (address)
            {
                case 0x04:
                    Div = 0;
                    break;
                case 0x05:
                    Tima = value;
                    break;
                case 0x06:
                    Tma = value;
                    break;
                case 0x07:
                    Tac = (TimerControlFlags) (value & 0b111);
                    break;
            }
        }

    }
}
