using System;
using Emux.Expressions;
using Emux.GameBoy.Cpu;

namespace Emux.Gui
{
    public class BreakpointInfo
    {
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
                    var predicate = ExpressionParser.CompileExpression(value);
                    Breakpoint.Condition = predicate;
                    _conditionString = value;
                }
            }
        }
    }
}