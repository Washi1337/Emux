using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Emux.GameBoy.Cpu;

namespace Emux.Expressions
{
    public class ExpressionParser
    {
        private static readonly Terminal[] OperatorPrecedence = 
        {
            Terminal.GreaterThan,
            Terminal.GreaterThanOrEqual,
            Terminal.LessThan,
            Terminal.LessThanOrEqual,

            Terminal.Equals,
            Terminal.NotEquals,
            
            Terminal.BitwiseAnd,
            Terminal.BitwiseOr,
            
            Terminal.LPar,
            Terminal.RPar
        };

        private static readonly ParameterExpression CpuParameter = Expression.Parameter(typeof(GameBoyCpu));
        
        public static Predicate<GameBoyCpu> CompileExpression(string code)
        {
            var lexer = new ExpressionLexer(new StringReader(code));
            
            var stack = new Stack<Expression>();
            foreach (var token in ToPostfix(lexer))
            {
                switch (token.Terminal)
                {
                    case Terminal.Register:
                        stack.Push(Expression.Convert(Expression.PropertyOrField(
                            Expression.Property(CpuParameter, "Registers"),
                            token.Text.ToUpperInvariant()), typeof(ushort)));
                        break;
                    
                    case Terminal.Hexadecimal:
                        var matches = Regex.Matches(token.Text, @"[\da-fA-F]+");
                        stack.Push(Expression.Constant(ushort.Parse(matches[matches.Count - 1].Value,
                            NumberStyles.HexNumber)));
                        break;
                    
                    case Terminal.Decimal:
                        stack.Push(Expression.Constant(ushort.Parse(token.Text)));
                        break;
                    
                    case Terminal.Equals:
                        stack.Push(Expression.Equal(stack.Pop(), stack.Pop()));
                        break;
                    
                    case Terminal.NotEquals:
                        stack.Push(Expression.NotEqual(stack.Pop(), stack.Pop()));
                        break;
                    
                    case Terminal.GreaterThan:
                        // Mirrored operator to accomodate for stack order.
                        stack.Push(Expression.LessThan(stack.Pop(), stack.Pop())); 
                        break;
                    
                    case Terminal.GreaterThanOrEqual:
                        // Mirrored operator to accomodate for stack order.
                        stack.Push(Expression.LessThanOrEqual(stack.Pop(), stack.Pop()));
                        break;
                    
                    case Terminal.LessThan:
                        // Mirrored operator to accomodate for stack order.
                        stack.Push(Expression.GreaterThan(stack.Pop(), stack.Pop()));
                        break;
                    
                    case Terminal.LessThanOrEqual:
                        // Mirrored operator to accomodate for stack order.
                        stack.Push(Expression.GreaterThanOrEqual(stack.Pop(), stack.Pop()));
                        break;
                    
                    case Terminal.BitwiseAnd:
                        stack.Push(Expression.And(stack.Pop(), stack.Pop()));
                        break;
                    
                    case Terminal.BitwiseOr:
                        stack.Push(Expression.Or(stack.Pop(), stack.Pop()));
                        break;
                    
                    case Terminal.BooleanAnd:
                        var v2 = stack.Pop();
                        var v1 = stack.Pop();
                        stack.Push(Expression.AndAlso(v1, v2));
                        break;
                    
                    case Terminal.BooleanOr:
                        v2 = stack.Pop();
                        v1 = stack.Pop();
                        stack.Push(Expression.OrElse(v1, v2));
                        break;
                }
            }

            if (stack.Count >= 2)
                throw new SyntaxErrorException("Expression contains unused terms.");
            
            var final = stack.Pop();
            var lambda = Expression.Lambda<Predicate<GameBoyCpu>>(final, CpuParameter);
            return lambda.Compile();
        }

        private static IEnumerable<Token> ToPostfix(ExpressionLexer lexer)
        {
            // Shunting yard algorithm to transform the infix expression into postfix for easier interpretation.
            
            var operatorStack = new Stack<Token>();
            while (lexer.HasNext())
            {
                var current = lexer.Next();
                switch (current.Terminal)
                {
                    case Terminal.Decimal:
                    case Terminal.Hexadecimal:
                    case Terminal.Flag:
                    case Terminal.Register:
                        yield return current;
                        break;
                    
                    case Terminal.LPar:
                        operatorStack.Push(current);
                        break;
                    
                    case Terminal.RPar:
                        while (operatorStack.Peek().Terminal != Terminal.LPar)
                            yield return operatorStack.Pop();
                        operatorStack.Pop(); // Pop LPar
                        break;
                    
                    default:
                        while (operatorStack.Count > 0)
                        {
                            var lastOperator = operatorStack.Peek();
                            int lastPrecedence = Array.IndexOf(OperatorPrecedence, lastOperator.Terminal);
                            int currentPrecedence = Array.IndexOf(OperatorPrecedence, current.Terminal);
                            if (lastPrecedence <= currentPrecedence)
                            {
                                yield return operatorStack.Pop();
                            }
                            else
                            {
                                break;
                            }
                            
                        }
                        operatorStack.Push(current);
                        break;
                }
            }

            while (operatorStack.Count > 0)
                yield return operatorStack.Pop();
        }
        
        
    }
}