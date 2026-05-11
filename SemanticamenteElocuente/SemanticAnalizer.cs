using System;
using System.Collections.Generic;
using System.Linq;

namespace SemanticamenteElocuente
{
    public sealed class SemanticAnalyzer
    {
        private readonly List<SemanticError> _diagnostics = new();
        private readonly List<SymbolInfo> _allDeclaredSymbols = new();

        private SemanticScope _currentScope = new SemanticScope();

        private int _loopDepth = 0;
        private int _switchDepth = 0;
        private int _functionDepth = 0;

        private SymbolInfo? _currentFunction;
        private Dictionary<string, int>? _currentFunctionParamMap;

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
            _diagnostics.Clear();
            _allDeclaredSymbols.Clear();

            _currentScope = new SemanticScope();
            _loopDepth = 0;
            _switchDepth = 0;
            _functionDepth = 0;
            _currentFunction = null;
            _currentFunctionParamMap = null;

            RegisterBuiltins();
            PredeclareTopLevelFunctions(program);

            foreach (var stmt in program.Statements)
                VisitStatement(stmt);

            EmitUnusedSymbolWarnings();

            return _diagnostics;
        }

        private void RegisterBuiltins()
        {
            var print = new SymbolInfo("print", SymbolKind.Function, SemanticType.Void, 1, true);
            print.ParameterTypes[0] = SemanticType.Unknown;
            DeclareSymbol(print);

            var sqrt = new SymbolInfo("sqrt", SymbolKind.Function, SemanticType.Number, 1, true);
            sqrt.ParameterTypes[0] = SemanticType.Number;
            DeclareSymbol(sqrt);

            var pow = new SymbolInfo("pow", SymbolKind.Function, SemanticType.Number, 2, true);
            pow.ParameterTypes[0] = SemanticType.Number;
            pow.ParameterTypes[1] = SemanticType.Number;
            DeclareSymbol(pow);

            var abs = new SymbolInfo("abs", SymbolKind.Function, SemanticType.Number, 1, true);
            abs.ParameterTypes[0] = SemanticType.Number;
            DeclareSymbol(abs);

            var len = new SymbolInfo("len", SymbolKind.Function, SemanticType.Number, 1, true);
            len.ParameterTypes[0] = SemanticType.String;
            DeclareSymbol(len);
            RegisterAsmBuiltins();

            // Función híbrida C# + Ensamblador
            var asmSuma = new SymbolInfo("asmSuma", SymbolKind.Function, SemanticType.Number, 2, true);
            asmSuma.ParameterTypes[0] = SemanticType.Number;
            asmSuma.ParameterTypes[1] = SemanticType.Number;
            DeclareSymbol(asmSuma);
        }

        private void PredeclareTopLevelFunctions(ProgramNode program)
        {
            foreach (var stmt in program.Statements)
            {
                if (stmt is not FunctionDecl fn)
                    continue;

                if (_currentScope.LookupLocal(fn.Name) != null)
                {
                    Error(fn, $"La función '{fn.Name}' ya está declarada en este ámbito.");
                    continue;
                }

                var fnSymbol = new SymbolInfo(
                    fn.Name,
                    SymbolKind.Function,
                    SemanticType.Unknown,
                    fn.Parameters.Count,
                    true,
                    fn.Line,
                    fn.Column);

                DeclareSymbol(fnSymbol);
            }
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

        private bool DeclareSymbol(SymbolInfo symbol)
        {
            bool ok = _currentScope.Declare(symbol);
            if (ok)
                _allDeclaredSymbols.Add(symbol);
            return ok;
        }

        private void Error(Node node, string message)
        {
            _diagnostics.Add(new SemanticError(node.Line, node.Column, message, SemanticSeverity.Error));
        }

        private void Warning(int line, int column, string message)
        {
            _diagnostics.Add(new SemanticError(line, column, message, SemanticSeverity.Warning));
        }

        private void EmitUnusedSymbolWarnings()
        {
            foreach (var symbol in _allDeclaredSymbols)
            {
                if (symbol.Kind == SymbolKind.Function)
                    continue;

                if (!symbol.WasUsed &&
                    symbol.DeclLine > 0 &&
                    symbol.DeclColumn > 0)
                {
                    string kindText = symbol.Kind switch
                    {
                        SymbolKind.Variable => "La variable",
                        SymbolKind.Constant => "La constante",
                        SymbolKind.Parameter => "El parámetro",
                        _ => "El símbolo"
                    };

                    Warning(symbol.DeclLine, symbol.DeclColumn,
                        $"{kindText} '{symbol.Name}' fue declarado pero nunca se utilizó.");
                }
            }
        }

        private void VisitStatement(Stmt stmt)
        {
            switch (stmt)
            {
                case BlockStmt block:
                    VisitBlock(block);
                    break;

                case ExprStmt exprStmt:
                    GetExprType(exprStmt.Expr);
                    break;

                case VarDecl varDecl:
                    VisitVarDecl(varDecl);
                    break;

                case AssignStmt assign:
                    VisitAssign(assign);
                    break;

                case PrintStmt print:
                    GetExprType(print.Expr);
                    break;

                case IfStmt ifStmt:
                    VisitIf(ifStmt);
                    break;

                case WhileStmt whileStmt:
                    VisitWhile(whileStmt);
                    break;

                case ForStmt forStmt:
                    VisitFor(forStmt);
                    break;

                case SwitchStmt switchStmt:
                    VisitSwitch(switchStmt);
                    break;

                case FunctionDecl fn:
                    VisitFunctionDecl(fn);
                    break;

                case ReturnStmt ret:
                    VisitReturn(ret);
                    break;

                case BreakStmt br:
                    if (_loopDepth == 0 && _switchDepth == 0)
                        Error(br, "break solo puede usarse dentro de while, for o switch.");
                    break;

                case ContinueStmt cont:
                    if (_loopDepth == 0)
                        Error(cont, "continue solo puede usarse dentro de while o for.");
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

        private void VisitVarDecl(VarDecl varDecl)
        {
            var kind = varDecl.Kind == "const"
                ? SymbolKind.Constant
                : SymbolKind.Variable;

            SemanticType initType = SemanticType.Unknown;
            bool initialized = false;

            if (varDecl.Init != null)
            {
                initType = GetExprType(varDecl.Init);
                initialized = true;
            }

            var symbol = new SymbolInfo(
                varDecl.Name,
                kind,
                initType,
                0,
                initialized,
                varDecl.Line,
                varDecl.Column);

            if (!DeclareSymbol(symbol))
            {
                Error(varDecl, $"'{varDecl.Name}' ya está declarado en este ámbito.");
            }
        }

        private void VisitAssign(AssignStmt assign)
        {
            var symbol = _currentScope.Lookup(assign.Name);

            if (symbol == null)
            {
                var suggestion = FindClosestName(assign.Name);

                if (!string.IsNullOrEmpty(suggestion))
                    Error(assign, $"La variable '{assign.Name}' no ha sido declarada. ¿Quisiste decir '{suggestion}'?");
                else
                    Error(assign, $"La variable '{assign.Name}' no ha sido declarada.");

                GetExprType(assign.Expr);
                return;
            }

            if (!symbol.IsAssignable)
            {
                Error(assign, $"No se puede modificar '{assign.Name}' porque es una constante.");
                GetExprType(assign.Expr);
                return;
            }

            var exprType = GetExprType(assign.Expr);

            if (symbol.Type == SemanticType.Unknown)
            {
                symbol.Type = exprType;
                PropagateParameterType(assign.Name, exprType);
            }
            else if (exprType != SemanticType.Unknown && symbol.Type != exprType)
            {
                Error(assign,
                    $"No se puede asignar un valor de tipo {TypeName(exprType)} a '{assign.Name}' porque ya fue inferida como {TypeName(symbol.Type)}.");
            }

            symbol.IsInitialized = true;
            symbol.WasAssigned = true;
        }

        private void VisitIf(IfStmt ifStmt)
        {
            var conditionType = GetExprType(ifStmt.Condition);

            if (conditionType != SemanticType.Bool && conditionType != SemanticType.Unknown)
                Error(ifStmt.Condition, "La condición de if debe ser booleana.");

            VisitStatement(ifStmt.ThenBranch);

            if (ifStmt.ElseBranch != null)
                VisitStatement(ifStmt.ElseBranch);
        }

        private void VisitWhile(WhileStmt whileStmt)
        {
            var conditionType = GetExprType(whileStmt.Condition);

            if (conditionType != SemanticType.Bool && conditionType != SemanticType.Unknown)
                Error(whileStmt.Condition, "La condición de while debe ser booleana.");

            _loopDepth++;
            VisitStatement(whileStmt.Body);
            _loopDepth--;
        }

        private void VisitFor(ForStmt forStmt)
        {
            PushScope();

            if (forStmt.Init != null)
                VisitStatement(forStmt.Init);

            if (forStmt.Condition != null)
            {
                var condType = GetExprType(forStmt.Condition);

                if (condType != SemanticType.Bool && condType != SemanticType.Unknown)
                    Error(forStmt.Condition, "La condición de for debe ser booleana.");
            }

            _loopDepth++;
            VisitStatement(forStmt.Body);

            if (forStmt.Increment != null)
                VisitStatement(forStmt.Increment);

            _loopDepth--;
            PopScope();
        }

        private void VisitSwitch(SwitchStmt switchStmt)
        {
            var switchType = GetExprType(switchStmt.Expr);

            _switchDepth++;

            var seenCases = new HashSet<string>();

            foreach (var c in switchStmt.Cases)
            {
                var caseType = GetExprType(c.Value);

                if (switchType != SemanticType.Unknown &&
                    caseType != SemanticType.Unknown &&
                    switchType != caseType)
                {
                    Error(c.Value,
                        $"El tipo del case ({TypeName(caseType)}) no coincide con el tipo del switch ({TypeName(switchType)}).");
                }

                string key = GetCaseKey(c.Value);
                if (!seenCases.Add(key))
                    Error(c.Value, "Valor de case duplicado en switch.");

                foreach (var st in c.Statements)
                    VisitStatement(st);
            }

            if (switchStmt.DefaultStatements != null)
            {
                foreach (var st in switchStmt.DefaultStatements)
                    VisitStatement(st);
            }

            _switchDepth--;
        }

        private void VisitFunctionDecl(FunctionDecl fn)
        {
            SymbolInfo? fnSymbol;

            if (_currentScope.Parent == null)
            {
                fnSymbol = _currentScope.LookupLocal(fn.Name);

                if (fnSymbol == null)
                {
                    fnSymbol = new SymbolInfo(
                        fn.Name,
                        SymbolKind.Function,
                        SemanticType.Unknown,
                        fn.Parameters.Count,
                        true,
                        fn.Line,
                        fn.Column);

                    if (!DeclareSymbol(fnSymbol))
                    {
                        Error(fn, $"La función '{fn.Name}' ya está declarada en este ámbito.");
                        return;
                    }
                }
            }
            else
            {
                if (_currentScope.LookupLocal(fn.Name) != null)
                {
                    Error(fn, $"La función '{fn.Name}' ya está declarada en este ámbito.");
                    return;
                }

                fnSymbol = new SymbolInfo(
                    fn.Name,
                    SymbolKind.Function,
                    SemanticType.Unknown,
                    fn.Parameters.Count,
                    true,
                    fn.Line,
                    fn.Column);

                DeclareSymbol(fnSymbol);
            }

            PushScope();
            _functionDepth++;

            var previousFunction = _currentFunction;
            var previousParamMap = _currentFunctionParamMap;

            _currentFunction = fnSymbol;
            _currentFunctionParamMap = new Dictionary<string, int>(StringComparer.Ordinal);

            var duplicated = fn.Parameters
                .GroupBy(p => p)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var dup in duplicated)
                Error(fn, $"El parámetro '{dup}' está duplicado en la función '{fn.Name}'.");

            for (int i = 0; i < fn.Parameters.Count; i++)
            {
                string paramName = fn.Parameters[i];

                if (_currentFunctionParamMap.ContainsKey(paramName))
                    continue;

                _currentFunctionParamMap[paramName] = i;

                var paramSymbol = new SymbolInfo(
                    paramName,
                    SymbolKind.Parameter,
                    fnSymbol.ParameterTypes[i],
                    0,
                    true,
                    fn.Line,
                    fn.Column);

                DeclareSymbol(paramSymbol);
            }

            VisitStatement(fn.Body);

            if (!fnSymbol.HasAnyReturn && fnSymbol.Type == SemanticType.Unknown)
                fnSymbol.Type = SemanticType.Void;

            _currentFunction = previousFunction;
            _currentFunctionParamMap = previousParamMap;

            _functionDepth--;
            PopScope();
        }

        private void VisitReturn(ReturnStmt ret)
        {
            if (_functionDepth == 0 || _currentFunction == null)
            {
                Error(ret, "return no puede usarse fuera de una función.");
                if (ret.Expr != null)
                    GetExprType(ret.Expr);
                return;
            }

            _currentFunction.HasAnyReturn = true;

            SemanticType returnType = ret.Expr == null
                ? SemanticType.Void
                : GetExprType(ret.Expr);

            if (_currentFunction.Type == SemanticType.Unknown)
            {
                _currentFunction.Type = returnType;
            }
            else if (returnType != SemanticType.Unknown && _currentFunction.Type != returnType)
            {
                Error(ret,
                    $"La función '{_currentFunction.Name}' retorna {TypeName(returnType)}, pero ya había sido inferida como {TypeName(_currentFunction.Type)}.");
            }
        }

        private SemanticType GetExprType(Expr expr)
        {
            switch (expr)
            {
                case NumberExpr:
                    return SemanticType.Number;

                case StringExpr:
                    return SemanticType.String;

                case BoolExpr:
                    return SemanticType.Bool;

                case IdentifierExpr id:
                    {
                        var symbol = _currentScope.Lookup(id.Name);

                        if (symbol == null)
                        {
                            var suggestion = FindClosestName(id.Name);

                            if (!string.IsNullOrEmpty(suggestion))
                                Error(id, $"Identificador '{id.Name}' no existe. ¿Quisiste decir '{suggestion}'?");
                            else
                                Error(id, $"Identificador '{id.Name}' no existe.");

                            return SemanticType.Unknown;
                        }

                        symbol.WasUsed = true;

                        if (symbol.Kind != SymbolKind.Function && !symbol.IsInitialized)
                        {
                            Error(id, $"El identificador '{id.Name}' se está usando antes de haber sido inicializado.");
                            return symbol.Type;
                        }

                        return symbol.Type;
                    }

                case UnaryExpr unary:
                    return GetUnaryExprType(unary);

                case BinaryExpr binary:
                    return GetBinaryExprType(binary);

                case CallExpr call:
                    return GetCallType(call);

                default:
                    return SemanticType.Unknown;
            }
        }

        private SemanticType GetUnaryExprType(UnaryExpr unary)
        {
            ConstrainExprToType(unary.Inner, unary.Op == UnaryOp.Not ? SemanticType.Bool : SemanticType.Number);

            var innerType = GetExprType(unary.Inner);

            switch (unary.Op)
            {
                case UnaryOp.Plus:
                case UnaryOp.Minus:
                    if (innerType != SemanticType.Number && innerType != SemanticType.Unknown)
                        Error(unary, $"El operador unario '{UnaryOpText(unary.Op)}' solo admite operandos numéricos.");
                    return SemanticType.Number;

                case UnaryOp.Not:
                    if (innerType != SemanticType.Bool && innerType != SemanticType.Unknown)
                        Error(unary, "El operador '!' solo admite operandos booleanos.");
                    return SemanticType.Bool;

                default:
                    return SemanticType.Unknown;
            }
        }

        private SemanticType GetBinaryExprType(BinaryExpr binary)
        {
            switch (binary.Op)
            {
                case BinaryOp.Sub:
                case BinaryOp.Mul:
                case BinaryOp.Div:
                case BinaryOp.Mod:
                case BinaryOp.Less:
                case BinaryOp.LessEqual:
                case BinaryOp.Greater:
                case BinaryOp.GreaterEqual:
                    ConstrainExprToType(binary.Left, SemanticType.Number);
                    ConstrainExprToType(binary.Right, SemanticType.Number);
                    break;

                case BinaryOp.And:
                case BinaryOp.Or:
                    ConstrainExprToType(binary.Left, SemanticType.Bool);
                    ConstrainExprToType(binary.Right, SemanticType.Bool);
                    break;
            }

            var left = GetExprType(binary.Left);
            var right = GetExprType(binary.Right);

            switch (binary.Op)
            {
                case BinaryOp.Add:
                    if (left == SemanticType.Number && right == SemanticType.Number)
                        return SemanticType.Number;

                    // Concatenación textual:
                    // string + number
                    // string + bool
                    // string + string
                    if (left == SemanticType.String || right == SemanticType.String)
                        return SemanticType.String;

                    if (left != SemanticType.Unknown && right != SemanticType.Unknown)
                        Error(binary,
                            $"El operador '+' solo admite suma numérica o concatenación con string, no {TypeName(left)}+{TypeName(right)}.");

                    return SemanticType.Unknown;

                case BinaryOp.Sub:
                case BinaryOp.Mul:
                case BinaryOp.Div:
                case BinaryOp.Mod:
                    if (left == SemanticType.Number && right == SemanticType.Number)
                        return SemanticType.Number;

                    if (left != SemanticType.Unknown && right != SemanticType.Unknown)
                        Error(binary,
                            $"El operador '{BinaryOpText(binary.Op)}' solo admite operandos numéricos, no {TypeName(left)} y {TypeName(right)}.");

                    return SemanticType.Unknown;

                case BinaryOp.Less:
                case BinaryOp.LessEqual:
                case BinaryOp.Greater:
                case BinaryOp.GreaterEqual:
                    if (left == SemanticType.Number && right == SemanticType.Number)
                        return SemanticType.Bool;

                    if (left != SemanticType.Unknown && right != SemanticType.Unknown)
                        Error(binary,
                            $"La comparación '{BinaryOpText(binary.Op)}' solo admite operandos numéricos, no {TypeName(left)} y {TypeName(right)}.");

                    return SemanticType.Bool;

                case BinaryOp.Equal:
                case BinaryOp.NotEqual:
                    if (left == SemanticType.Unknown || right == SemanticType.Unknown)
                        return SemanticType.Bool;

                    if (left != right)
                        Error(binary,
                            $"No se pueden comparar valores de tipos distintos: {TypeName(left)} y {TypeName(right)}.");

                    return SemanticType.Bool;

                case BinaryOp.And:
                case BinaryOp.Or:
                    if (left == SemanticType.Bool && right == SemanticType.Bool)
                        return SemanticType.Bool;

                    if (left != SemanticType.Unknown && right != SemanticType.Unknown)
                        Error(binary,
                            $"El operador lógico '{BinaryOpText(binary.Op)}' solo admite booleanos, no {TypeName(left)} y {TypeName(right)}.");

                    return SemanticType.Bool;

                default:
                    return SemanticType.Unknown;
            }
        }

        private SemanticType GetCallType(CallExpr call)
        {
            var symbol = _currentScope.Lookup(call.Name);

            if (symbol == null)
            {
                var suggestion = FindClosestName(call.Name);

                if (!string.IsNullOrEmpty(suggestion))
                    Error(call, $"Función '{call.Name}' no declarada. ¿Quisiste decir '{suggestion}'?");
                else
                    Error(call, $"Función '{call.Name}' no declarada.");

                foreach (var arg in call.Arguments)
                    GetExprType(arg);

                return SemanticType.Unknown;
            }

            if (symbol.Kind != SymbolKind.Function)
            {
                Error(call, $"'{call.Name}' no es una función.");

                foreach (var arg in call.Arguments)
                    GetExprType(arg);

                return SemanticType.Unknown;
            }

            if (symbol.Arity != call.Arguments.Count)
            {
                Error(call,
                    $"La función '{call.Name}' espera {symbol.Arity} argumento(s), pero recibió {call.Arguments.Count}.");
            }

            int count = Math.Min(symbol.ParameterTypes.Count, call.Arguments.Count);

            for (int i = 0; i < count; i++)
            {
                var argType = GetExprType(call.Arguments[i]);
                var paramType = symbol.ParameterTypes[i];

                if (paramType == SemanticType.Unknown && argType != SemanticType.Unknown)
                {
                    symbol.ParameterTypes[i] = argType;
                }
                else if (paramType != SemanticType.Unknown &&
                         argType != SemanticType.Unknown &&
                         paramType != argType)
                {
                    Error(call,
                        $"El argumento {i + 1} de '{call.Name}' debe ser {TypeName(paramType)}, pero recibió {TypeName(argType)}.");
                }
            }

            for (int i = count; i < call.Arguments.Count; i++)
                GetExprType(call.Arguments[i]);

            ValidateBuiltinCall(call);

            return symbol.Type;
        }

        private void ValidateBuiltinCall(CallExpr call)
        {
            if (call.Name == "print")
                return;

            if (call.Name == "sqrt" || call.Name == "abs")
            {
                if (call.Arguments.Count == 1)
                {
                    var t = GetExprType(call.Arguments[0]);
                    if (t != SemanticType.Number && t != SemanticType.Unknown)
                        Error(call, $"La función '{call.Name}' requiere un argumento numérico.");
                }
                return;
            }

            if (call.Name == "pow")
            {
                if (call.Arguments.Count == 2)
                {
                    var t1 = GetExprType(call.Arguments[0]);
                    var t2 = GetExprType(call.Arguments[1]);

                    if (t1 != SemanticType.Number && t1 != SemanticType.Unknown)
                        Error(call, "El primer argumento de 'pow' debe ser numérico.");

                    if (t2 != SemanticType.Number && t2 != SemanticType.Unknown)
                        Error(call, "El segundo argumento de 'pow' debe ser numérico.");
                }
                return;
            }

            if (call.Name == "len")
            {
                if (call.Arguments.Count == 1)
                {
                    var t = GetExprType(call.Arguments[0]);
                    if (t != SemanticType.String && t != SemanticType.Unknown)
                        Error(call, "La función 'len' requiere un argumento de tipo string.");
                }
            }
        }

        private void ConstrainExprToType(Expr expr, SemanticType expectedType)
        {
            if (expr is IdentifierExpr id)
            {
                var symbol = _currentScope.Lookup(id.Name);
                if (symbol == null)
                    return;

                if (symbol.Type == SemanticType.Unknown)
                {
                    symbol.Type = expectedType;
                    PropagateParameterType(id.Name, expectedType);
                }
                else if (symbol.Type != expectedType)
                {
                    Error(id,
                        $"El identificador '{id.Name}' debe ser de tipo {TypeName(expectedType)}, pero es {TypeName(symbol.Type)}.");
                }
            }
        }

        private void PropagateParameterType(string name, SemanticType type)
        {
            if (_currentFunction == null || _currentFunctionParamMap == null)
                return;

            if (!_currentFunctionParamMap.TryGetValue(name, out int index))
                return;

            if (index < 0 || index >= _currentFunction.ParameterTypes.Count)
                return;

            var current = _currentFunction.ParameterTypes[index];

            if (current == SemanticType.Unknown)
            {
                _currentFunction.ParameterTypes[index] = type;
            }
            else if (type != SemanticType.Unknown && current != type)
            {
                Warning(_currentFunction.DeclLine, _currentFunction.DeclColumn,
                    $"El parámetro '{name}' de la función '{_currentFunction.Name}' recibe inferencias incompatibles ({TypeName(current)} y {TypeName(type)}).");
            }
        }

        private string GetCaseKey(Expr expr)
        {
            return expr switch
            {
                NumberExpr n => $"num:{n.Value}",
                StringExpr s => $"str:{s.Value}",
                BoolExpr b => $"bool:{b.Value}",
                _ => $"expr:{expr.GetType().Name}:{expr.Line}:{expr.Column}"
            };
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
        private void RegisterAsmBuiltins()
        {
            DeclareBuiltinNumberFunction("asmSuma", SemanticType.Number, SemanticType.Number);
            DeclareBuiltinNumberFunction("asmMax", SemanticType.Number, SemanticType.Number);
            DeclareBuiltinNumberFunction("asmFactorial", SemanticType.Number);
            DeclareBuiltinNumberFunction("asmEsPar", SemanticType.Number);
        }

        private void DeclareBuiltinNumberFunction(string name, params SemanticType[] parameterTypes)
        {
            var symbol = new SymbolInfo(
                name,
                SymbolKind.Function,
                SemanticType.Number,
                parameterTypes.Length,
                true);

            for (int i = 0; i < parameterTypes.Length; i++)
                symbol.ParameterTypes[i] = parameterTypes[i];

            DeclareSymbol(symbol);
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

        private static string TypeName(SemanticType type)
        {
            return type switch
            {
                SemanticType.Number => "number",
                SemanticType.String => "string",
                SemanticType.Bool => "bool",
                SemanticType.Void => "void",
                _ => "unknown"
            };
        }

        private static string BinaryOpText(BinaryOp op)
        {
            return op switch
            {
                BinaryOp.Add => "+",
                BinaryOp.Sub => "-",
                BinaryOp.Mul => "*",
                BinaryOp.Div => "/",
                BinaryOp.Mod => "%",
                BinaryOp.Equal => "==",
                BinaryOp.NotEqual => "!=",
                BinaryOp.Less => "<",
                BinaryOp.LessEqual => "<=",
                BinaryOp.Greater => ">",
                BinaryOp.GreaterEqual => ">=",
                BinaryOp.And => "&&",
                BinaryOp.Or => "||",
                _ => "?"
            };
        }

        private static string UnaryOpText(UnaryOp op)
        {
            return op switch
            {
                UnaryOp.Plus => "+",
                UnaryOp.Minus => "-",
                UnaryOp.Not => "!",
                _ => "?"
            };
        }
    }
}