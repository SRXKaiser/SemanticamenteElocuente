using System;
using System.Collections.Generic;
using System.Linq;

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

        // Para variables/constantes/parámetros = tipo del símbolo.
        // Para funciones = tipo de retorno.
        public SemanticType Type { get; set; }

        public List<SemanticType> ParameterTypes { get; }

        public bool IsInitialized { get; set; }
        public bool WasUsed { get; set; }
        public bool WasAssigned { get; set; }
        public bool HasAnyReturn { get; set; }

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

            ParameterTypes = arity > 0
                ? Enumerable.Repeat(SemanticType.Unknown, arity).ToList()
                : new List<SemanticType>();
        }

        public bool IsAssignable =>
            Kind == SymbolKind.Variable || Kind == SymbolKind.Parameter;
    }
}