using System;
using System.Collections.Generic;
using System.Threading;
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
		public const float OfficialFrameRate = 59.7f;

		private readonly ManualResetEvent _continueSignal = new ManualResetEvent(false);
		private readonly ManualResetEvent _terminateSignal = new ManualResetEvent(false);
		private readonly ManualResetEvent _frameStartSignal = new ManualResetEvent(false);
		private readonly ManualResetEvent _breakSignal = new ManualResetEvent(false);

		/// <summary>
		/// Occurs when the processor has resumed execution.
		/// </summary>
		public event EventHandler Resumed;

		/// <summary>
		/// Occurs when the processor is paused by breaking the execution explicitly, or when the control flow hit a breakpoint.
		/// </summary>
		public event EventHandler Paused;

		/// <summary>
		/// Occurs when the process has completely shut down.
		/// </summary>
		public event EventHandler Terminated;

		public event StepEventHandler PerformedStep;

		private readonly IDictionary<ushort, Breakpoint> _breakpoints = new Dictionary<ushort, Breakpoint>();

		private IClock Clock;

		private int _framesCount;

		public double FramesPerSecond;
		public bool EnableFrameLimit = false;
		public TimeSpan FrameDelta;
		private DateTime lastFrame;

		private TimeSpan _frameStartTime;


		public GameBoy(ICartridge cartridge, IClock clock, bool preferGbcMode)
        {
            GbcMode = preferGbcMode && (cartridge.GameBoyColorFlag & GameBoyColorFlag.SupportsColor) != 0;

			Clock = clock;

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

			Clock.Tick += nextFrame;
			new Thread(CpuLoop)
			{
				Name = "Z80CPULOOP",
				IsBackground = true
			}.Start();


			lastFrame = DateTime.Now;
			Gpu.VBlankStarted += (_, __) =>
			{
				_framesCount++;

				FrameDelta = DateTime.Now - lastFrame;
				lastFrame = DateTime.Now;
			};
		}

        private ICollection<IGameBoyComponent> Components
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

		public double SpeedFactor => Cpu.CyclesPerSecond / (GameBoyCpu.OfficialClockFrequency * Cpu.SpeedMultiplier);

		private void nextFrame(object sender, EventArgs e)
		{
			_frameStartSignal.Set();

			var time = DateTime.Now.TimeOfDay;
			var delta = time - _frameStartTime;
			if (delta.TotalSeconds > 1)
			{
				FramesPerSecond = _framesCount / delta.TotalSeconds;
				_framesCount = 0;
				Cpu.SecondElapsed(delta);

				_frameStartTime = time;
			}
		}

		public void Step()
		{
			Clock.Stop();
			Cpu.IsBroken = true;
			_continueSignal.Set();
		}

		public void Run()
		{
			_frameStartTime = DateTime.Now.TimeOfDay;
			Clock.Start();
			Cpu.IsBroken = false;
			_continueSignal.Set();
		}

		public void Break()
		{
			_breakSignal.Set();
			Clock.Stop();
			_continueSignal.Reset();
			Cpu.IsBroken = true;
		}

		private void CpuLoop()
		{
			bool enabled = true;
			while (enabled)
			{
				if (WaitHandle.WaitAny(new WaitHandle[] { _continueSignal, _terminateSignal }) == 1)
				{
					enabled = false;
				}
				else
				{
					Cpu.Running = true;
					_continueSignal.Reset();
					OnResumed();

					int cycles = 0;
					do
					{
						var elapsedCycles = Cpu.PerformNextInstruction();

						onPerformedStep(elapsedCycles / Cpu.SpeedMultiplier);

						cycles += elapsedCycles;
						if (cycles >= GameBoyGpu.FullFrameCycles * Cpu.SpeedMultiplier)
						{
							Spu.SpuStep(cycles / Cpu.SpeedMultiplier);
							cycles -= GameBoyGpu.FullFrameCycles * Cpu.SpeedMultiplier;
							if (EnableFrameLimit)
							{
								WaitHandle.WaitAny(new WaitHandle[] { _breakSignal, _frameStartSignal });
								_frameStartSignal.Reset();
							}
						}

						if (_breakpoints.TryGetValue(Cpu.Registers.PC, out var breakpoint) && breakpoint.Condition(Cpu))
							Cpu.IsBroken = true;

					} while (!Cpu.IsBroken);

					_breakSignal.Reset();
					Cpu.Running = false;
					OnPaused();
				}
			}
			OnTerminated();
		}

		private void onPerformedStep(int cycles)
		{
			do
			{
				Gpu.Step(1);
				Timer.Step(1);
				Memory.DmaController.Step();
			} while (--cycles > 0);
		}

		protected virtual void OnResumed()
		{
			Resumed?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void OnPaused()
		{
			Paused?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void OnTerminated()
		{
			Terminated?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void OnPerformedStep(StepEventArgs args)
		{
			PerformedStep?.Invoke(this, args);
		}

		public Breakpoint SetBreakpoint(ushort address)
		{
			if (!_breakpoints.TryGetValue(address, out var breakpoint))
			{
				breakpoint = new Breakpoint(address);
				_breakpoints.Add(address, breakpoint);
			}

			return breakpoint;
		}

		public void RemoveBreakpoint(ushort address)
		{
			_breakpoints.Remove(address);
		}

		public IEnumerable<Breakpoint> GetBreakpoints()
		{
			return _breakpoints.Values;
		}

		public Breakpoint GetBreakpointAtAddress(ushort address)
		{
			_breakpoints.TryGetValue(address, out var breakpoint);
			return breakpoint;
		}

		public void ClearBreakpoints()
		{
			_breakpoints.Clear();
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
			Break();
			Clock.Stop();

            foreach (var component in Components)
                component.Shutdown();
            IsPoweredOn = false;
        }
	}
}
