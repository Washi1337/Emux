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
        public GameBoy(ICartridge cartridge, IClock clock, bool preferGbcMode)
        {
            GbcMode = preferGbcMode && (cartridge.GameBoyColorFlag & GameBoyColorFlag.SupportsColor) != 0;

            Components = new List<IGameBoyComponent>
            {
                (Cartridge = cartridge),
                (Memory = new GameBoyMemory(this)),
                (Cpu = new GameBoyCpu(this, clock)),
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
