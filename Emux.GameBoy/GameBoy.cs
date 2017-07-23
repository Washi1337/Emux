using System;
using Emux.GameBoy.Cpu;
using Emux.GameBoy.Graphics;
using Emux.GameBoy.Input;
using Emux.GameBoy.Memory;

namespace Emux.GameBoy
{
    public class GameBoy
    {
        public GameBoy(Cartridge cartridge)
        {
            Cartridge = cartridge;
            Cpu = new GameBoyCpu(this);
            Gpu = new GameBoyGpu(this);
            Memory = new GameBoyMemory(this);
            KeyPad = new GameBoyPad(this);
            Reset();
            IsPoweredOn = true;
        }

        public GameBoyCpu Cpu
        {
            get;
        }

        public GameBoyGpu Gpu
        {
            get;
        }

        public GameBoyMemory Memory
        {
            get;
        }

        public Cartridge Cartridge
        {
            get;
        }

        public GameBoyPad KeyPad
        {
            get;
        }

        public bool IsPoweredOn
        {
            get;
            private set;
        }

        public void Reset()
        {
            Gpu.Reset();
            
            Cpu.Registers.A = 0x01;
            Cpu.Registers.F = 0xB0;
            Cpu.Registers.BC = 0x0013;
            Cpu.Registers.DE = 0x00D8;
            Cpu.Registers.HL = 0x014D;
            Cpu.Registers.PC = 0x100;
            Cpu.Registers.IE = 0;
            Cpu.Registers.IF = (InterruptFlags) 0xE1;
            Cpu.Registers.IME = false;

            Memory.WriteByte(0xFF05, 0x00);
            Memory.WriteByte(0xFF06, 0x00);
            Memory.WriteByte(0xFF07, 0x00);
            Memory.WriteByte(0xFF10, 0x80);
            Memory.WriteByte(0xFF11, 0xBF);
            Memory.WriteByte(0xFF12, 0xF3);
            Memory.WriteByte(0xFF14, 0xBF);
            Memory.WriteByte(0xFF16, 0x3F);
            Memory.WriteByte(0xFF17, 0x00);
            Memory.WriteByte(0xFF19, 0xBF);
            Memory.WriteByte(0xFF1A, 0x7F);
            Memory.WriteByte(0xFF1B, 0xFF);
            Memory.WriteByte(0xFF1C, 0x9F);
            Memory.WriteByte(0xFF1E, 0xBF);
            Memory.WriteByte(0xFF20, 0xFF);
            Memory.WriteByte(0xFF21, 0x00);
            Memory.WriteByte(0xFF22, 0x00);
            Memory.WriteByte(0xFF23, 0xBF);
            Memory.WriteByte(0xFF24, 0x77);
            Memory.WriteByte(0xFF25, 0xF3);
            Memory.WriteByte(0xFF26, 0xF1);
            Memory.WriteByte(0xFF40, 0x91);
            Memory.WriteByte(0xFF42, 0x00);
            Memory.WriteByte(0xFF43, 0x00);
            Memory.WriteByte(0xFF45, 0x00);
            Memory.WriteByte(0xFF47, 0xFC);
            Memory.WriteByte(0xFF48, 0xFF);
            Memory.WriteByte(0xFF49, 0xFF);
            Memory.WriteByte(0xFF4A, 0x00);
            Memory.WriteByte(0xFF4B, 0x00);
            Memory.WriteByte(0xFFFF, 0x00);
        }

        public void Terminate()
        {
            Cpu.Terminate();
            IsPoweredOn = false;
        }
    }
}
