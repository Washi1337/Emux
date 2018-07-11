namespace Emux.Expressions
{
    public class Token
    {
        public Token(Terminal terminal, string text)
        {
            Terminal = terminal;
            Text = text;
        }

        public Terminal Terminal
        {
            get;
        }

        public string Text
        {
            get;
        }

        public override string ToString()
        {
            return $"{Text} ({Terminal})";
        }
    }
}