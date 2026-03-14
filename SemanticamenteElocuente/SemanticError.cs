using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticamenteElocuente
{
    public sealed class SemanticError
    {
        public int Line { get; }
        public int Column { get; }
        public string Message { get; }

        public SemanticError(int line, int column, string message)
        {
            Line = line;
            Column = column;
            Message = message;
        }

        public override string ToString()
        {
            return $"[L{Line},C{Column}] {Message}";
        }
    }
}
