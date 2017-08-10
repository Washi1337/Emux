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
            get { return _gameBoy.Cpu.Breakpoints.Contains(_instruction.Offset); }
            set
            {
                if (value)
                    _gameBoy.Cpu.Breakpoints.Add(_instruction.Offset);
                else
                    _gameBoy.Cpu.Breakpoints.Remove(_instruction.Offset);
                OnPropertyChanged(nameof(IsBreakpoint));
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


        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
