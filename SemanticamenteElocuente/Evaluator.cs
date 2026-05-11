using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SemanticamenteElocuente
{
    public sealed class EvalError : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public EvalError(string msg, int line, int column) : base(msg)
        {
            Line = line;
            Column = column;
        }

        public override string ToString() => $"[L{Line},C{Column}] {Message}";
    }

    internal sealed class ReturnSignal : Exception
    {
        public object? Value { get; }
        public ReturnSignal(object? value) => Value = value;
    }

    internal sealed class BreakSignal : Exception { }
    internal sealed class ContinueSignal : Exception { }

    public sealed class RuntimeEnvironment
    {
        private sealed class VariableEntry
        {
            public object? Value;
            public bool IsConst;
        }

        private readonly Dictionary<string, VariableEntry> _variables = new(StringComparer.Ordinal);
        private readonly Dictionary<string, FunctionDecl> _functions = new(StringComparer.Ordinal);

        public RuntimeEnvironment? Parent { get; }

        public RuntimeEnvironment(RuntimeEnvironment? parent = null)
        {
            Parent = parent;
        }

        public void Declare(string kind, string name, object? value, int line, int column)
        {
            if (_variables.ContainsKey(name))
                throw new EvalError($"'{name}' ya está declarado en este ámbito.", line, column);

            _variables[name] = new VariableEntry
            {
                Value = value,
                IsConst = string.Equals(kind, "const", StringComparison.Ordinal)
            };
        }

        public void Assign(string name, object? value, int line, int column)
        {
            if (_variables.TryGetValue(name, out var entry))
            {
                if (entry.IsConst)
                    throw new EvalError($"No se puede asignar a la constante '{name}'.", line, column);

                entry.Value = value;
                return;
            }

            if (Parent is not null)
            {
                Parent.Assign(name, value, line, column);
                return;
            }

            throw new EvalError($"Variable no declarada: '{name}'.", line, column);
        }

        public object? Get(string name, int line, int column)
        {
            if (_variables.TryGetValue(name, out var entry))
                return entry.Value;

            if (Parent is not null)
                return Parent.Get(name, line, column);

            throw new EvalError($"Identificador no definido: '{name}'.", line, column);
        }

        public void DefineFunction(FunctionDecl fn)
        {
            if (_functions.ContainsKey(fn.Name))
                throw new EvalError($"La función '{fn.Name}' ya está declarada.", fn.Line, fn.Column);

            _functions[fn.Name] = fn;
        }

        public FunctionDecl GetFunction(string name, int line, int column)
        {
            if (_functions.TryGetValue(name, out var fn))
                return fn;

            if (Parent is not null)
                return Parent.GetFunction(name, line, column);

            throw new EvalError($"Función no declarada: '{name}'.", line, column);
        }

        public IReadOnlyDictionary<string, object?> SnapshotCurrentScope()
        {
            var copy = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var kv in _variables)
                copy[kv.Key] = kv.Value.Value;
            return copy;
        }
    }

    public sealed class Evaluator
    {
        private readonly RuntimeEnvironment _global;
        private readonly List<string> _output = new();

        public Evaluator(RuntimeEnvironment? env = null)
        {
            _global = env ?? new RuntimeEnvironment();
        }

        public List<string> Run(ProgramNode program)
        {
            _output.Clear();

            foreach (var stmt in program.Statements)
                Execute(stmt, _global);

            return new List<string>(_output);
        }

        public IReadOnlyDictionary<string, object?> Snapshot()
            => _global.SnapshotCurrentScope();

        private void Execute(Stmt stmt, RuntimeEnvironment env)
        {
            switch (stmt)
            {
                case BlockStmt block:
                    ExecuteBlock(block, new RuntimeEnvironment(env));
                    return;

                case ExprStmt exprStmt:
                    Evaluate(exprStmt.Expr, env);
                    return;

                case VarDecl v:
                    {
                        object? init = v.Init is null ? null : Evaluate(v.Init, env);
                        env.Declare(v.Kind, v.Name, init, v.Line, v.Column);
                        return;
                    }

                case AssignStmt a:
                    {
                        object? value = Evaluate(a.Expr, env);
                        env.Assign(a.Name, value, a.Line, a.Column);
                        return;
                    }

                case PrintStmt p:
                    {
                        object? value = Evaluate(p.Expr, env);
                        _output.Add(FormatValue(value));
                        return;
                    }

                case IfStmt i:
                    {
                        if (IsTruthy(Evaluate(i.Condition, env)))
                            Execute(i.ThenBranch, env);
                        else if (i.ElseBranch is not null)
                            Execute(i.ElseBranch, env);

                        return;
                    }

                case WhileStmt w:
                    {
                        while (IsTruthy(Evaluate(w.Condition, env)))
                        {
                            try
                            {
                                Execute(w.Body, env);
                            }
                            catch (ContinueSignal)
                            {
                                continue;
                            }
                            catch (BreakSignal)
                            {
                                break;
                            }
                        }

                        return;
                    }

                case ForStmt f:
                    {
                        var forEnv = new RuntimeEnvironment(env);

                        if (f.Init is not null)
                            Execute(f.Init, forEnv);

                        while (f.Condition is null || IsTruthy(Evaluate(f.Condition, forEnv)))
                        {
                            try
                            {
                                Execute(f.Body, forEnv);
                            }
                            catch (ContinueSignal)
                            {
                                // continuar con incremento
                            }
                            catch (BreakSignal)
                            {
                                break;
                            }

                            if (f.Increment is not null)
                                Execute(f.Increment, forEnv);
                        }

                        return;
                    }

                case SwitchStmt s:
                    {
                        object? switchValue = Evaluate(s.Expr, env);
                        bool matched = false;

                        foreach (var sc in s.Cases)
                        {
                            object? caseValue = Evaluate(sc.Value, env);
                            if (!matched && AreEqualValues(switchValue, caseValue))
                                matched = true;

                            if (matched)
                            {
                                try
                                {
                                    foreach (var inner in sc.Statements)
                                        Execute(inner, env);
                                }
                                catch (BreakSignal)
                                {
                                    return;
                                }
                            }
                        }

                        if (!matched && s.DefaultStatements is not null)
                        {
                            try
                            {
                                foreach (var inner in s.DefaultStatements)
                                    Execute(inner, env);
                            }
                            catch (BreakSignal)
                            {
                                return;
                            }
                        }

                        return;
                    }

                case FunctionDecl fn:
                    env.DefineFunction(fn);
                    return;

                case ReturnStmt r:
                    {
                        object? value = r.Expr is null ? null : Evaluate(r.Expr, env);
                        throw new ReturnSignal(value);
                    }

                case BreakStmt:
                    throw new BreakSignal();

                case ContinueStmt:
                    throw new ContinueSignal();

                default:
                    throw new EvalError($"Sentencia no soportada: {stmt.GetType().Name}", stmt.Line, stmt.Column);
            }
        }

        private void ExecuteBlock(BlockStmt block, RuntimeEnvironment env)
        {
            foreach (var stmt in block.Statements)
                Execute(stmt, env);
        }

        private object? Evaluate(Expr expr, RuntimeEnvironment env)
        {
            switch (expr)
            {
                case NumberExpr n:
                    return n.Value;

                case StringExpr s:
                    return s.Value;

                case BoolExpr b:
                    return b.Value;

                case IdentifierExpr id:
                    return env.Get(id.Name, id.Line, id.Column);

                case UnaryExpr u:
                    return EvalUnary(u, env);

                case BinaryExpr b:
                    return EvalBinary(b, env);

                case CallExpr c:
                    return EvalCall(c, env);

                default:
                    throw new EvalError($"Expresión no soportada: {expr.GetType().Name}", expr.Line, expr.Column);
            }
        }

        private object? EvalUnary(UnaryExpr u, RuntimeEnvironment env)
        {
            object? value = Evaluate(u.Inner, env);

            return u.Op switch
            {
                UnaryOp.Plus => ToNumber(value, u.Line, u.Column),
                UnaryOp.Minus => -ToNumber(value, u.Line, u.Column),
                UnaryOp.Not => !IsTruthy(value),
                _ => throw new EvalError("Operador unario desconocido.", u.Line, u.Column)
            };
        }

        private object? EvalBinary(BinaryExpr b, RuntimeEnvironment env)
        {
            if (b.Op == BinaryOp.And)
            {
                object? left = Evaluate(b.Left, env);
                if (!IsTruthy(left))
                    return false;

                object? right = Evaluate(b.Right, env);
                return IsTruthy(right);
            }

            if (b.Op == BinaryOp.Or)
            {
                object? left = Evaluate(b.Left, env);
                if (IsTruthy(left))
                    return true;

                object? right = Evaluate(b.Right, env);
                return IsTruthy(right);
            }

            object? L = Evaluate(b.Left, env);
            object? R = Evaluate(b.Right, env);

            return b.Op switch
            {
                BinaryOp.Add => AddValues(L, R, b.Line, b.Column),
                BinaryOp.Sub => ToNumber(L, b.Line, b.Column) - ToNumber(R, b.Line, b.Column),
                BinaryOp.Mul => ToNumber(L, b.Line, b.Column) * ToNumber(R, b.Line, b.Column),
                BinaryOp.Div => DivideValues(L, R, b.Line, b.Column),
                BinaryOp.Mod => ModValues(L, R, b.Line, b.Column),

                BinaryOp.Equal => AreEqualValues(L, R),
                BinaryOp.NotEqual => !AreEqualValues(L, R),
                BinaryOp.Less => ToNumber(L, b.Line, b.Column) < ToNumber(R, b.Line, b.Column),
                BinaryOp.LessEqual => ToNumber(L, b.Line, b.Column) <= ToNumber(R, b.Line, b.Column),
                BinaryOp.Greater => ToNumber(L, b.Line, b.Column) > ToNumber(R, b.Line, b.Column),
                BinaryOp.GreaterEqual => ToNumber(L, b.Line, b.Column) >= ToNumber(R, b.Line, b.Column),

                _ => throw new EvalError("Operador binario desconocido.", b.Line, b.Column)
            };
        }

        private object? EvalCall(CallExpr c, RuntimeEnvironment env)
        {
            // Built-ins simples
            if (string.Equals(c.Name, "sqrt", StringComparison.Ordinal))
            {
                if (c.Arguments.Count != 1)
                    throw new EvalError("sqrt requiere exactamente 1 argumento.", c.Line, c.Column);

                return Math.Sqrt(ToNumber(Evaluate(c.Arguments[0], env), c.Line, c.Column));
            }

            if (string.Equals(c.Name, "pow", StringComparison.Ordinal))
            {
                if (c.Arguments.Count != 2)
                    throw new EvalError("pow requiere exactamente 2 argumentos.", c.Line, c.Column);

                double x = ToNumber(Evaluate(c.Arguments[0], env), c.Line, c.Column);
                double y = ToNumber(Evaluate(c.Arguments[1], env), c.Line, c.Column);
                return Math.Pow(x, y);
            }

            if (string.Equals(c.Name, "abs", StringComparison.Ordinal))
            {
                if (c.Arguments.Count != 1)
                    throw new EvalError("abs requiere exactamente 1 argumento.", c.Line, c.Column);

                return Math.Abs(ToNumber(Evaluate(c.Arguments[0], env), c.Line, c.Column));
            }

            if (string.Equals(c.Name, "len", StringComparison.Ordinal))
            {
                if (c.Arguments.Count != 1)
                    throw new EvalError("len requiere exactamente 1 argumento.", c.Line, c.Column);

                object? value = Evaluate(c.Arguments[0], env);
                return FormatValue(value).Length;
            }

            if (string.Equals(c.Name, "asmSuma", StringComparison.Ordinal))
            {
                if (c.Arguments.Count != 2)
                    throw new EvalError("asmSuma requiere exactamente 2 argumentos.", c.Line, c.Column);

                int a = ToInt32ForAsm(Evaluate(c.Arguments[0], env), c.Line, c.Column);
                int b = ToInt32ForAsm(Evaluate(c.Arguments[1], env), c.Line, c.Column);

                int resultado = NativeAsm.SumaAsm(a, b);

                return (double)resultado;
            }
            if (TryEvalAsmCall(c, env, out var asmResult))
                return asmResult;
            var fn = env.GetFunction(c.Name, c.Line, c.Column);

            if (fn.Parameters.Count != c.Arguments.Count)
                throw new EvalError(
                    $"La función '{fn.Name}' esperaba {fn.Parameters.Count} argumento(s), pero recibió {c.Arguments.Count}.",
                    c.Line, c.Column);

            var callEnv = new RuntimeEnvironment(env);

            for (int i = 0; i < fn.Parameters.Count; i++)
            {
                object? argValue = Evaluate(c.Arguments[i], env);
                callEnv.Declare("let", fn.Parameters[i], argValue, c.Line, c.Column);
            }

            try
            {
                Execute(fn.Body, callEnv);
                return null;
            }
            catch (ReturnSignal ret)
            {
                return ret.Value;
            }
        }

        private static object AddValues(object? left, object? right, int line, int col)
        {
            if (left is string || right is string)
                return $"{FormatValue(left)}{FormatValue(right)}";

            return ToNumber(left, line, col) + ToNumber(right, line, col);
        }

        private static object DivideValues(object? left, object? right, int line, int col)
        {
            double divisor = ToNumber(right, line, col);
            if (Math.Abs(divisor) < 1e-12)
                throw new EvalError("División entre cero.", line, col);

            return ToNumber(left, line, col) / divisor;
        }

        private static object ModValues(object? left, object? right, int line, int col)
        {
            double divisor = ToNumber(right, line, col);
            if (Math.Abs(divisor) < 1e-12)
                throw new EvalError("Módulo entre cero.", line, col);

            return ToNumber(left, line, col) % divisor;
        }

        private static bool AreEqualValues(object? a, object? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;

            if (a is double da && b is double db)
                return Math.Abs(da - db) < 1e-12;

            if (a is bool ba && b is bool bb)
                return ba == bb;

            return string.Equals(Convert.ToString(a, CultureInfo.InvariantCulture),
                                 Convert.ToString(b, CultureInfo.InvariantCulture),
                                 StringComparison.Ordinal);
        }

        private static double ToNumber(object? value, int line, int col)
        {
            return value switch
            {
                double d => d,
                int i => i,
                long l => l,
                bool b => b ? 1d : 0d,
                string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) => n,
                _ => throw new EvalError($"No se puede convertir a número: '{FormatValue(value)}'.", line, col)
            };
        }
        private static int ToInt32ForAsm(object? value, int line, int col)
        {
            double number = ToNumber(value, line, col);

            if (number % 1 != 0)
                throw new EvalError("asmSuma solo acepta números enteros.", line, col);

            if (number < int.MinValue || number > int.MaxValue)
                throw new EvalError("asmSuma solo acepta enteros de 32 bits.", line, col);

            return (int)number;
        }
        private bool TryEvalAsmCall(CallExpr c, RuntimeEnvironment env, out object? result)
        {
            result = null;

            switch (c.Name)
            {
                case "asmSuma":
                    {
                        EnsureArgCount(c, 2);

                        int a = ToInt32ForAsm(Evaluate(c.Arguments[0], env), c.Line, c.Column);
                        int b = ToInt32ForAsm(Evaluate(c.Arguments[1], env), c.Line, c.Column);

                        result = (double)NativeAsm.SumaAsm(a, b);
                        return true;
                    }

                case "asmMax":
                    {
                        EnsureArgCount(c, 2);

                        int a = ToInt32ForAsm(Evaluate(c.Arguments[0], env), c.Line, c.Column);
                        int b = ToInt32ForAsm(Evaluate(c.Arguments[1], env), c.Line, c.Column);

                        result = (double)NativeAsm.MaxAsm(a, b);
                        return true;
                    }

                case "asmFactorial":
                    {
                        EnsureArgCount(c, 1);

                        int n = ToInt32ForAsm(Evaluate(c.Arguments[0], env), c.Line, c.Column);
                        int value = NativeAsm.FactorialAsm(n);

                        if (value == -1)
                            throw new EvalError("asmFactorial solo acepta valores entre 0 y 12.", c.Line, c.Column);

                        result = (double)value;
                        return true;
                    }

                case "asmEsPar":
                    {
                        EnsureArgCount(c, 1);

                        int n = ToInt32ForAsm(Evaluate(c.Arguments[0], env), c.Line, c.Column);

                        result = NativeAsm.EsParAsm(n) == 1;
                        return true;
                    }

                default:
                    return false;
            }
        }

        private static void EnsureArgCount(CallExpr c, int expected)
        {
            if (c.Arguments.Count != expected)
                throw new EvalError(
                    $"{c.Name} requiere exactamente {expected} argumento(s).",
                    c.Line,
                    c.Column);
        }

        private static bool IsTruthy(object? value)
        {
            return value switch
            {
                null => false,
                bool b => b,
                double d => Math.Abs(d) > 1e-12,
                int i => i != 0,
                long l => l != 0,
                string s => !string.IsNullOrEmpty(s),
                _ => true
            };
        }

        private static string FormatValue(object? value)
        {
            return value switch
            {
                null => "null",
                bool b => b ? "true" : "false",
                double d => Math.Abs(d % 1) < 1e-12
                    ? ((long)Math.Round(d)).ToString(CultureInfo.InvariantCulture)
                    : d.ToString(CultureInfo.InvariantCulture),
                float f => Math.Abs(f % 1) < 1e-6
                    ? ((long)Math.Round(f)).ToString(CultureInfo.InvariantCulture)
                    : f.ToString(CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null"
            };
        }
    }
}
