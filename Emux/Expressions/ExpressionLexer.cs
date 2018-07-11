using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Emux.Expressions
{
    public class ExpressionLexer
    {
        private static readonly ISet<string> Registers = new HashSet<string>
        {
            "PC", "SP", "AF", "BC", "DE", "HL", "A", "B", "C", "D", "E", "F", "H", "L",
            "pc", "sp", "af", "bc", "de", "hl", "a", "b", "c", "d", "e", "f", "h", "l"
        };

        private const string HexCharacters = "0123456789ABCDEF";
        
        private readonly TextReader _reader;
        private Token _bufferedToken;
        
        public ExpressionLexer(TextReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public bool HasNext()
        {
            SkipWhitespaces();
            return _reader.Peek() != -1;
        }

        public Token Peek()
        {
            if (_bufferedToken == null && HasNext())
                ReadNextToken();

            return _bufferedToken;
        }

        public Token Next()
        {
            if (_bufferedToken == null)
                ReadNextToken();
            var token = _bufferedToken;
            _bufferedToken = null;
            return token;
        }

        private void SkipWhitespaces()
        {
            while (true)
            {
                int c = _reader.Peek();
                if (c == -1 || !char.IsWhiteSpace((char) c))
                    break;
                _reader.Read();
            }
        }

        private void ReadNextToken()
        {
            if (!HasNext())
                throw new EndOfStreamException();

            char c = (char) _reader.Peek();

            _bufferedToken = char.IsLetterOrDigit(c) ? ReadNextWord() : ReadNextSymbol();
        }

        private Token ReadNextWord()
        {
            string word = ReadWhile(char.IsLetterOrDigit);
            Terminal terminal;
            
            if (Registers.Contains(word))
                terminal = Terminal.Register;
            else if (Regex.IsMatch(word, @"(0x[\da-zA-Z]+)|([\da-zA-Z]+h)"))
                terminal = Terminal.Hexadecimal;
            else if (Regex.IsMatch(word, @"\d+"))
                terminal = Terminal.Decimal;
            else 
                throw new SyntaxErrorException("Unrecognized word '" + word + "'.");

            return new Token(terminal, word);
        }

        private string ReadWhile(Predicate<char> condition)
        {
            var builder = new StringBuilder();

            while (true)
            {
                int p = _reader.Peek();
                if (p == -1)
                    break;

                char c = (char) p;
                if (!condition(c))
                    break;

                _reader.Read();
                builder.Append(c);
            } 
            
            return builder.ToString();
        }


        private Token ReadNextSymbol()
        {
            char c = (char) _reader.Read();
            switch (c)
            {
                case '(':
                    return new Token(Terminal.LPar, "(");
                
                case ')':
                    return new Token(Terminal.RPar, ")");
                        
                case '!':
                    return new Token(Terminal.Not, "!");
                
                case '+':
                    return new Token(Terminal.Plus, "+");
                
                case '-':
                    return new Token(Terminal.Minus, "-");
                
                case '=':
                    return new Token(Terminal.Equals, "=");
                
                case '>':
                    if (_reader.Peek() != '=')
                        return new Token(Terminal.GreaterThan, "=");
                    _reader.Read();
                    return new Token(Terminal.GreaterThanOrEqual, ">=");

                case '<':
                    if (_reader.Peek() != '=')
                        return new Token(Terminal.Equals, "=");
                    _reader.Read();
                    return new Token(Terminal.LessThanOrEqual, ">=");

                case '&':
                    if (_reader.Peek() != '&')
                        return new Token(Terminal.BitwiseAnd, "&");
                    _reader.Read();
                    return new Token(Terminal.BooleanAnd, "&&");

                case '|':
                    if (_reader.Peek() != '|')
                        return new Token(Terminal.BitwiseOr, "|");
                    _reader.Read();
                    return new Token(Terminal.BooleanOr, "||");

            }
            
            throw new SyntaxErrorException("Unrecognized character '" + c + "'.");
        }
    }
}