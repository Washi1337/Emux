using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Emux.GameBoy.Cpu;

namespace Emux.Gui
{
    public class InstructionItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private readonly GameBoy.GameBoy _gameBoy;
        private readonly Z80Instruction _instruction;
        
        public InstructionItem(GameBoy.GameBoy gameBoy, Z80Instruction instruction)
        {
            if (gameBoy == null)
                throw new ArgumentNullException(nameof(gameBoy));
            if (instruction == null)
                throw new ArgumentNullException(nameof(instruction));
            _gameBoy = gameBoy;
            _instruction = instruction;
        }

        public bool IsBreakpoint
        {
            get { return Breakpoint != null; }
            set
            {
                if (value)
                    _gameBoy.SetBreakpoint(_instruction.Offset);
                else
                    _gameBoy.RemoveBreakpoint(_instruction.Offset);
                OnPropertyChanged(nameof(IsBreakpoint));
            }
        }

        public BreakpointInfo Breakpoint
        {
            get
            {
                var bp = _gameBoy.GetBreakpointAtAddress(_instruction.Offset);
                if (bp == null)
                    return null;
                App.Current.DeviceManager.Breakpoints.TryGetValue(_instruction.Offset, out var breakpointInfo);
                if (breakpointInfo == null || breakpointInfo.Breakpoint != bp)
                {
                    breakpointInfo = new BreakpointInfo(bp);
                    breakpointInfo.PropertyChanged += (sender, args) => OnPropertyChanged(nameof(IsBreakpoint));
                }

                return breakpointInfo;
            }
        }

        public bool IsCurrentInstruction
        {
            get { return _gameBoy.Cpu.Registers.PC == Offset; }
        }

        public bool IsReturn
        {
            get { return _instruction.OpCode.Disassembly.StartsWith("ret"); }
        }

        public bool IsCall
        {
            get { return _instruction.OpCode.Disassembly.StartsWith("call"); }
        }

        public bool IsJump
        {
            get { return _instruction.OpCode.Disassembly.StartsWith("j"); }
        }

        public int Offset
        {
            get { return _instruction.Offset; }
        }

        public string Disassembly
        {
            get { return _instruction.Disassembly; }
        }

        public int Cycles
        {
            get { return _instruction.OpCode.ClockCycles; }
        }

        public string Comment
        {
            get;
            set;
        }
        
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
