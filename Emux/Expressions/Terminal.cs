namespace Emux.Expressions
{
    public enum Terminal
    {
        Not, 
        
        Equals,
        NotEquals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        
        Plus,
        Minus,
        
        BooleanAnd,
        BitwiseAnd,
        BooleanOr,
        BitwiseOr,
        
        Register,
        Flag,
        
        Decimal,
        Hexadecimal,
        RPar,
        LPar
    }
}