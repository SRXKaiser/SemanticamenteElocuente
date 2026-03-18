using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticamenteElocuente
{
    public enum SemanticSeverity
    {
        Error,
        Warning
    }

    public sealed class SemanticError
    {
        public int Line { get; }
        public int Column { get; }
        public string Message { get; }
        public SemanticSeverity Severity { get; }

        public SemanticError(int line, int column, string message, SemanticSeverity severity = SemanticSeverity.Error)
        {
            Line = line;
            Column = column;
            Message = message;
            Severity = severity;
        }

        public override string ToString()
        {
            string prefix = Severity == SemanticSeverity.Warning ? "Warning" : "Error";
            return $"[{prefix}] [L{Line},C{Column}] {Message}";
        }
    }
}