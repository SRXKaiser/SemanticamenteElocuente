using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticamenteElocuente
{
    public enum SymbolKind
    {
        Variable,
        Constant,
        Function,
        Parameter
    }

    public sealed class SymbolInfo
    {
        public string Name { get; }
        public SymbolKind Kind { get; }
        public int Arity { get; }
        public SemanticType Type { get; set; }

        public SymbolInfo(string name, SymbolKind kind, SemanticType type = SemanticType.Unknown, int arity = 0)
        {
            Name = name;
            Kind = kind;
            Type = type;
            Arity = arity;
        }

        public bool IsAssignable =>
            Kind == SymbolKind.Variable || Kind == SymbolKind.Parameter;
    }
}
