using System;
using System.ComponentModel;
using Emux.Expressions;
using Emux.GameBoy.Cpu;

namespace Emux.Gui
{
    public class BreakpointInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private string _conditionString;

        public BreakpointInfo(Breakpoint breakpoint)
        {
            Breakpoint = breakpoint ?? throw new ArgumentNullException(nameof(breakpoint));
        }
        
        public Breakpoint Breakpoint
        {
            get;
        }
        
        public ushort Address
        {
            get { return Breakpoint.Offset; }
        }

        public string ConditionString
        {
            get { return _conditionString; }
            set
            {
                if (_conditionString != value)
                {
                    Breakpoint.Condition = string.IsNullOrEmpty(value)
                        ? Breakpoint.BreakAlways
                        : ExpressionParser.CompileExpression(value);

                    _conditionString = value;
                    OnPropertyChanged(nameof(ConditionString));
                }
            }
        }
        
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}