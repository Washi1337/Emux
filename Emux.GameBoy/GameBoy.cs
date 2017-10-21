using System.Collections.Generic;
using Emux.GameBoy.Audio;
using Emux.GameBoy.Cartridge;
using Emux.GameBoy.Cpu;
using Emux.GameBoy.Graphics;
using Emux.GameBoy.Input;
using Emux.GameBoy.Memory;
using Emux.GameBoy.Timer;

namespace Emux.GameBoy
{
    /// <summary>
    /// Represents an emulated game boy device. This includes the processor chip, the graphics chip, the memory controller 
    /// </summary>
    public class GameBoy
    {
        public GameBoy(ICartridge cartridge, bool preferGbcMode)
        {
            Cartridge = cartridge;

            GbcMode = preferGbcMode && (cartridge.GameBoyColorFlag & GameBoyColorFlag.SupportsColor) != 0;

            Components = new List<IGameBoyComponent>
            {
                (Memory = new GameBoyMemory(this)),
                (Cpu = new GameBoyCpu(this)),
                (Gpu = new GameBoyGpu(this)),
                (Spu = new GameBoySpu(this)),
                (KeyPad = new GameBoyPad(this)),
                (Timer = new GameBoyTimer(this))
            }.AsReadOnly();
            
            foreach (var component in Components)
                component.Initialize();

            Reset();
            IsPoweredOn = true;
        }

        public ICollection<IGameBoyComponent> Components
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether the GameBoy device is in GameBoy Color (GBC) mode, enabling specific features only GameBoy Color devices have.
        /// </summary>
        public bool GbcMode
        {
            get;
        }

        /// <summary>
        /// Gets the central processing unit of the emulated GameBoy device.
        /// </summary>
        public GameBoyCpu Cpu
        {
            get;
        }

        /// <summary>
        /// Gets the graphics processing unit of the emulated GameBoy device.
        /// </summary>
        public GameBoyGpu Gpu
        {
            get;
        }

        /// <summary>
        /// Gets the sound processing unit of the emulated GameBoy device.
        /// </summary>
        public GameBoySpu Spu
        {
            get;
        }

        /// <summary>
        /// Gets the memory controller of the emulated GameBoy device.
        /// </summary>
        public GameBoyMemory Memory
        {
            get;
        }

        /// <summary>
        /// Gets the cartridge that is inserted into the GameBoy.
        /// </summary>
        public ICartridge Cartridge
        {
            get;
        }

        /// <summary>
        /// Gets the keypad driver of the GameBoy device.
        /// </summary>
        public GameBoyPad KeyPad
        {
            get;
        }

        /// <summary>
        /// Gets the timer driver of the GameBoy device.
        /// </summary>
        public GameBoyTimer Timer
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether the GameBoy device is powered on.
        /// </summary>
        public bool IsPoweredOn
        {
            get;
            private set;
        }

        /// <summary>
        /// Resets the state of the GameBoy to the bootup state.
        /// </summary>
        public void Reset()
        {
            foreach (var component in Components)
                component.Reset();

            Cpu.Registers.A = GbcMode ? (byte) 0x11 : (byte) 0x01;
            Cpu.Registers.F = 0xB0;
            Cpu.Registers.BC = 0x0013;
            Cpu.Registers.DE = 0x00D8;
            Cpu.Registers.HL = 0x014D;
            Cpu.Registers.PC = 0x100;
            Cpu.Registers.SP = 0xFFFE;
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

        /// <summary>
        /// Shuts down the GameBoy device.
        /// </summary>
        public void Terminate()
        {
            foreach (var component in Components)
                component.Shutdown();
            IsPoweredOn = false;
        }
    }
}
