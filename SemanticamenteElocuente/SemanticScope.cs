using System;
using System.Collections.Generic;
using System.Text;


namespace SemanticamenteElocuente
{
    public sealed class SemanticScope
    {
        private readonly Dictionary<string, SymbolInfo> _symbols = new();

        public SemanticScope? Parent { get; }

        public SemanticScope(SemanticScope? parent = null)
        {
            Parent = parent;
        }


        public bool Declare(SymbolInfo symbol)
        {
            if (_symbols.ContainsKey(symbol.Name))
                return false;

            _symbols[symbol.Name] = symbol;
            return true;
        }

        public SymbolInfo? LookupLocal(string name)
        {
            _symbols.TryGetValue(name, out var symbol);
            return symbol;
        }

        public SymbolInfo? Lookup(string name)
        {
            if (_symbols.TryGetValue(name, out var symbol))
                return symbol;

            return Parent?.Lookup(name);
        }
        public IEnumerable<string> GetDeclaredNames()
        {
            return _symbols.Keys;
        }
    }
}
