using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticamenteElocuente
{
    public sealed class SemanticAnalyzer
    {
        private readonly List<SemanticError> _errors = new();

        private SemanticScope _currentScope = new SemanticScope();

        private int _loopDepth = 0;
        private int _switchDepth = 0;
        private int _functionDepth = 0;
        private static readonly string[] ReservedWords =
        {
            "var",
            "let",
            "const",
            "print",
            "if",
            "else",
            "while",
            "for",
            "switch",
            "case",
            "default",
            "break",
            "continue",
            "function",
            "return",
            "true",
            "false"
        };
        public IReadOnlyList<SemanticError> Analyze(ProgramNode program)
        {
            _errors.Clear();

            _currentScope = new SemanticScope();
            _loopDepth = 0;
            _switchDepth = 0;
            _functionDepth = 0;

            RegisterBuiltins();

            foreach (var stmt in program.Statements)
                VisitStatement(stmt);

            return _errors;
        }
        

        private void RegisterBuiltins()
        {
            _currentScope.Declare(new SymbolInfo("print", SymbolKind.Function, 1));
            _currentScope.Declare(new SymbolInfo("sqrt", SymbolKind.Function, 1));
            _currentScope.Declare(new SymbolInfo("pow", SymbolKind.Function, 2));
            _currentScope.Declare(new SymbolInfo("abs", SymbolKind.Function, 1));
            _currentScope.Declare(new SymbolInfo("len", SymbolKind.Function, 1));
        }


        private void PushScope()
        {
            _currentScope = new SemanticScope(_currentScope);
        }

        private void PopScope()
        {
            if (_currentScope.Parent != null)
                _currentScope = _currentScope.Parent;
        }

        private void Error(Node node, string message)
        {
            _errors.Add(new SemanticError(node.Line, node.Column, message));
        }

        private void VisitStatement(Stmt stmt)
        {
            switch (stmt)
            {
                case BlockStmt block:
                    VisitBlock(block);
                    break;

                case ExprStmt expr:
                    VisitExpression(expr.Expr);
                    break;

                case VarDecl v:
                    VisitVarDecl(v);
                    break;

                case AssignStmt a:
                    VisitAssign(a);
                    break;

                case PrintStmt p:
                    VisitExpression(p.Expr);
                    break;

                case IfStmt i:
                    VisitExpression(i.Condition);
                    VisitStatement(i.ThenBranch);
                    if (i.ElseBranch != null)
                        VisitStatement(i.ElseBranch);
                    break;

                case WhileStmt w:

                    VisitExpression(w.Condition);

                    _loopDepth++;

                    VisitStatement(w.Body);

                    _loopDepth--;

                    break;

                case ForStmt f:

                    PushScope();

                    if (f.Init != null)
                        VisitStatement(f.Init);

                    if (f.Condition != null)
                        VisitExpression(f.Condition);

                    _loopDepth++;

                    VisitStatement(f.Body);

                    if (f.Increment != null)
                        VisitStatement(f.Increment);

                    _loopDepth--;

                    PopScope();

                    break;

                case SwitchStmt s:

                    VisitExpression(s.Expr);

                    _switchDepth++;

                    var seenCases = new HashSet<string>();

                    foreach (var c in s.Cases)
                    {
                        VisitExpression(c.Value);

                        string key = c.Value.ToString();

                        if (!seenCases.Add(key))
                            Error(c.Value, "case duplicado en switch");

                        foreach (var st in c.Statements)
                            VisitStatement(st);
                    }

                    if (s.DefaultStatements != null)
                    {
                        foreach (var st in s.DefaultStatements)
                            VisitStatement(st);
                    }

                    _switchDepth--;

                    break;

                case FunctionDecl fn:

                    if (!_currentScope.Declare(
                        new SymbolInfo(fn.Name, SymbolKind.Function, fn.Parameters.Count)))
                    {
                        Error(fn, $"La función '{fn.Name}' ya está declarada.");
                        return;
                    }

                    PushScope();

                    _functionDepth++;

                    foreach (var p in fn.Parameters)
                        _currentScope.Declare(new SymbolInfo(p, SymbolKind.Parameter));

                    VisitStatement(fn.Body);

                    _functionDepth--;

                    PopScope();

                    break;

                case ReturnStmt r:

                    if (_functionDepth == 0)
                        Error(r, "return fuera de función");

                    if (r.Expr != null)
                        VisitExpression(r.Expr);

                    break;

                case BreakStmt b:

                    if (_loopDepth == 0 && _switchDepth == 0)
                        Error(b, "break fuera de ciclo o switch");

                    break;

                case ContinueStmt c:

                    if (_loopDepth == 0)
                        Error(c, "continue fuera de ciclo");

                    break;
            }
        }

        private void VisitBlock(BlockStmt block)
        {
            PushScope();

            foreach (var stmt in block.Statements)
                VisitStatement(stmt);

            PopScope();
        }

        private void VisitVarDecl(VarDecl v)
        {
            var kind = v.Kind == "const"
                ? SymbolKind.Constant
                : SymbolKind.Variable;

            if (!_currentScope.Declare(new SymbolInfo(v.Name, kind)))
                Error(v, $"'{v.Name}' ya está declarado.");

            if (v.Init != null)
                VisitExpression(v.Init);
        }

        private void VisitAssign(AssignStmt a)
        {
            var symbol = _currentScope.Lookup(a.Name);

            if (symbol == null)
                Error(a, $"Variable '{a.Name}' no declarada.");

            else if (!symbol.IsAssignable)
                Error(a, $"No se puede modificar '{a.Name}'.");

            VisitExpression(a.Expr);
        }

        private void VisitExpression(Expr expr)
        {
            switch (expr)
            {
                case IdentifierExpr id:
                    {
                        if (_currentScope.Lookup(id.Name) == null)
                        {
                            var suggestion = FindClosestName(id.Name);

                            if (!string.IsNullOrEmpty(suggestion))
                                Error(id, $"Identificador '{id.Name}' no existe. ¿Quisiste decir '{suggestion}'?");
                            else
                                Error(id, $"Identificador '{id.Name}' no existe.");
                        }

                        break;
                    }

                case BinaryExpr b:

                    VisitExpression(b.Left);
                    VisitExpression(b.Right);

                    break;

                case UnaryExpr u:

                    VisitExpression(u.Inner);

                    break;

                case CallExpr call:

                    VisitCall(call);

                    break;
            }
        }

        private void VisitCall(CallExpr call)
        {
            var symbol = _currentScope.Lookup(call.Name);

            if (symbol == null)
            {
                var suggestion = FindClosestName(call.Name);

                if (!string.IsNullOrEmpty(suggestion))
                    Error(call, $"Función '{call.Name}' no declarada. ¿Quisiste decir '{suggestion}'?");
                else
                    Error(call, $"Función '{call.Name}' no declarada.");
            }
            else if (symbol.Kind != SymbolKind.Function)
                Error(call, $"'{call.Name}' no es función.");

            else if (symbol.Arity != call.Arguments.Count)
                Error(call, $"'{call.Name}' espera {symbol.Arity} argumentos.");

            foreach (var arg in call.Arguments)
                VisitExpression(arg);
        }
        private string? FindClosestName(string name)
        {
            var candidates = new HashSet<string>(ReservedWords);

            CollectScopeSymbols(_currentScope, candidates);

            string? best = null;
            int bestDistance = int.MaxValue;

            foreach (var candidate in candidates)
            {
                int dist = LevenshteinDistance(name, candidate);

                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    best = candidate;
                }
            }

            // Umbral pequeño para evitar sugerencias absurdas
            return bestDistance <= 2 ? best : null;
        }

        private void CollectScopeSymbols(SemanticScope? scope, HashSet<string> names)
        {
            while (scope != null)
            {
                foreach (var name in scope.GetDeclaredNames())
                    names.Add(name);

                scope = scope.Parent;
            }
        }

        private static int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            int[,] dp = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++)
                dp[i, 0] = i;

            for (int j = 0; j <= b.Length; j++)
                dp[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;

                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                        dp[i - 1, j - 1] + cost
                    );
                }
            }

            return dp[a.Length, b.Length];
        }
    }
}
