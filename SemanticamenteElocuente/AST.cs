using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticamenteElocuente
{
    public abstract class Node
    {
        public int Line { get; }
        public int Column { get; }

        protected Node(int line, int column)
        {
            Line = line;
            Column = column;
        }
    }

    // =========================
    // EXPRESIONES
    // =========================
    public abstract class Expr : Node
    {
        protected Expr(int l, int c) : base(l, c) { }
    }

    public sealed class NumberExpr : Expr
    {
        public double Value { get; }
        public NumberExpr(double value, int l, int c) : base(l, c) => Value = value;
    }

    public sealed class StringExpr : Expr
    {
        public string Value { get; }
        public StringExpr(string value, int l, int c) : base(l, c) => Value = value;
    }

    public sealed class BoolExpr : Expr
    {
        public bool Value { get; }
        public BoolExpr(bool value, int l, int c) : base(l, c) => Value = value;
    }

    public sealed class IdentifierExpr : Expr
    {
        public string Name { get; }
        public IdentifierExpr(string name, int l, int c) : base(l, c) => Name = name;
    }

    public enum UnaryOp
    {
        Plus,
        Minus,
        Not
    }

    public sealed class UnaryExpr : Expr
    {
        public UnaryOp Op { get; }
        public Expr Inner { get; }

        public UnaryExpr(UnaryOp op, Expr inner, int l, int c) : base(l, c)
        {
            Op = op;
            Inner = inner;
        }
    }

    public enum BinaryOp
    {
        Add,
        Sub,
        Mul,
        Div,
        Mod,

        Equal,
        NotEqual,
        Less,
        LessEqual,
        Greater,
        GreaterEqual,

        And,
        Or
    }

    public sealed class BinaryExpr : Expr
    {
        public Expr Left { get; }
        public BinaryOp Op { get; }
        public Expr Right { get; }

        public BinaryExpr(Expr left, BinaryOp op, Expr right, int l, int c) : base(l, c)
        {
            Left = left;
            Op = op;
            Right = right;
        }
    }

    public sealed class CallExpr : Expr
    {
        public string Name { get; }
        public List<Expr> Arguments { get; }

        public CallExpr(string name, List<Expr> arguments, int l, int c) : base(l, c)
        {
            Name = name;
            Arguments = arguments;
        }
    }

    // =========================
    // SENTENCIAS
    // =========================
    public abstract class Stmt : Node
    {
        protected Stmt(int l, int c) : base(l, c) { }
    }

    public sealed class BlockStmt : Stmt
    {
        public List<Stmt> Statements { get; }
        public BlockStmt(List<Stmt> statements, int l, int c) : base(l, c) => Statements = statements;
    }

    public sealed class ExprStmt : Stmt
    {
        public Expr Expr { get; }
        public ExprStmt(Expr expr, int l, int c) : base(l, c) => Expr = expr;
    }

    public sealed class VarDecl : Stmt
    {
        public string Kind { get; }
        public string Name { get; }
        public Expr? Init { get; }

        public VarDecl(string kind, string name, Expr? init, int l, int c) : base(l, c)
        {
            Kind = kind;
            Name = name;
            Init = init;
        }
    }

    public sealed class AssignStmt : Stmt
    {
        public string Name { get; }
        public Expr Expr { get; }

        public AssignStmt(string name, Expr expr, int l, int c) : base(l, c)
        {
            Name = name;
            Expr = expr;
        }
    }

    public sealed class PrintStmt : Stmt
    {
        public Expr Expr { get; }
        public PrintStmt(Expr expr, int l, int c) : base(l, c) => Expr = expr;
    }

    public sealed class IfStmt : Stmt
    {
        public Expr Condition { get; }
        public Stmt ThenBranch { get; }
        public Stmt? ElseBranch { get; }

        public IfStmt(Expr condition, Stmt thenBranch, Stmt? elseBranch, int l, int c) : base(l, c)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }
    }

    public sealed class WhileStmt : Stmt
    {
        public Expr Condition { get; }
        public Stmt Body { get; }

        public WhileStmt(Expr condition, Stmt body, int l, int c) : base(l, c)
        {
            Condition = condition;
            Body = body;
        }
    }

    public sealed class ForStmt : Stmt
    {
        public Stmt? Init { get; }
        public Expr? Condition { get; }
        public Stmt? Increment { get; }
        public Stmt Body { get; }

        public ForStmt(Stmt? init, Expr? condition, Stmt? increment, Stmt body, int l, int c) : base(l, c)
        {
            Init = init;
            Condition = condition;
            Increment = increment;
            Body = body;
        }
    }

    public sealed class SwitchCase
    {
        public Expr Value { get; }
        public List<Stmt> Statements { get; }

        public SwitchCase(Expr value, List<Stmt> statements)
        {
            Value = value;
            Statements = statements;
        }
    }

    public sealed class SwitchStmt : Stmt
    {
        public Expr Expr { get; }
        public List<SwitchCase> Cases { get; }
        public List<Stmt>? DefaultStatements { get; }

        public SwitchStmt(Expr expr, List<SwitchCase> casesList, List<Stmt>? defaultStatements, int l, int c) : base(l, c)
        {
            Expr = expr;
            Cases = casesList;
            DefaultStatements = defaultStatements;
        }
    }

    public sealed class FunctionDecl : Stmt
    {
        public string Name { get; }
        public List<string> Parameters { get; }
        public BlockStmt Body { get; }

        public FunctionDecl(string name, List<string> parameters, BlockStmt body, int l, int c) : base(l, c)
        {
            Name = name;
            Parameters = parameters;
            Body = body;
        }
    }

    public sealed class ReturnStmt : Stmt
    {
        public Expr? Expr { get; }
        public ReturnStmt(Expr? expr, int l, int c) : base(l, c) => Expr = expr;
    }

    public sealed class BreakStmt : Stmt
    {
        public BreakStmt(int l, int c) : base(l, c) { }
    }

    public sealed class ContinueStmt : Stmt
    {
        public ContinueStmt(int l, int c) : base(l, c) { }
    }

    public sealed class ProgramNode : Node
    {
        public IReadOnlyList<Stmt> Statements { get; }
        public ProgramNode(List<Stmt> stmts) : base(1, 1) => Statements = stmts;
    }
}
