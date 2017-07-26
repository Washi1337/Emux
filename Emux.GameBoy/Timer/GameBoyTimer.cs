using System;
using Emux.GameBoy.Cpu;

namespace Emux.GameBoy.Timer
{
    public class GameBoyTimer
    {
        private readonly GameBoy _device;
        public const int DivTimerSpeed = 16384;

        private int _divClock;
        private int _timerClock;

        public byte Div;
        public byte Tima;
        public byte Tma;
        public TimerControlFlags Tac;

        public GameBoyTimer(GameBoy device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            _device = device;
        }

        public int GetTimaSpeed()
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

        public void TimerStep(int cycles)
        {
            _divClock += cycles;
            if (_divClock >= DivTimerSpeed)
            {
                _divClock -= DivTimerSpeed;
                Div = (byte) ((Div + 1) % 0xFF);
            }

            if ((Tac & TimerControlFlags.EnableTimer) == TimerControlFlags.EnableTimer)
            {
                _timerClock += cycles;
                int timaSpeed = GetTimaSpeed();
                if (_timerClock > timaSpeed)
                {
                    _timerClock -= timaSpeed;
                    Tima = Tma;
                    _device.Cpu.Registers.IF |= InterruptFlags.Timer;   
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
