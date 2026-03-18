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

        public bool IsInitialized { get; set; }
        public bool WasUsed { get; set; }
        public bool WasAssigned { get; set; }

        public int DeclLine { get; }
        public int DeclColumn { get; }

        public SymbolInfo(
            string name,
            SymbolKind kind,
            SemanticType type = SemanticType.Unknown,
            int arity = 0,
            bool isInitialized = false,
            int declLine = 0,
            int declColumn = 0)
        {
            Name = name;
            Kind = kind;
            Type = type;
            Arity = arity;
            IsInitialized = isInitialized;
            DeclLine = declLine;
            DeclColumn = declColumn;
        }

        public bool IsAssignable =>
            Kind == SymbolKind.Variable || Kind == SymbolKind.Parameter;
    }
}
