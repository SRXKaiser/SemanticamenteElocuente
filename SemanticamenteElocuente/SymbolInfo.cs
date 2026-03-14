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

        public SymbolInfo(string name, SymbolKind kind, int arity = 0)
        {
            Name = name;
            Kind = kind;
            Arity = arity;
        }

        public bool IsAssignable =>
            Kind == SymbolKind.Variable ||
            Kind == SymbolKind.Parameter;
    }
}
