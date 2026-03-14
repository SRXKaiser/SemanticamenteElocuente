using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticamenteElocuente
{
    public sealed class Token
    {
        public TokenType Type { get; }
        public string Lexeme { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(TokenType type, string lexeme, int line, int column)
        {
            Type = type;
            Lexeme = lexeme;
            Line = line;
            Column = column;
        }

        public override string ToString()
            => $"{Line}:{Column}  {Type,-12}  '{Lexeme}'";
    }
}
