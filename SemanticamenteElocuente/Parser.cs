using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using static SemanticamenteElocuente.Parser;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SemanticamenteElocuente
{
    public sealed class ParseError : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public ParseError(string msg, int line, int column) : base(msg)
        {
            Line = line;
            Column = column;
        }

        public override string ToString() => $"[L{Line},C{Column}] {Message}";
    }

    public sealed class Parser
    {
        private readonly List<Token> _tokens;
        private int _i;
        public List<ParseDiagnostic> Errors { get; } = new();

        public Parser(IEnumerable<Token> tokens)
        {
            _tokens = new List<Token>(tokens);
        }

        private Token T => _i < _tokens.Count ? _tokens[_i] : _tokens[^1];

        private bool Check(TokenType type)
            => _i < _tokens.Count && _tokens[_i].Type == type;

        private bool Match(TokenType type)
        {
            if (Check(type))
            {
                _i++;
                return true;
            }
            return false;
        }

        private Token Expect(TokenType type, string msg)
        {
            if (Check(type))
                return _tokens[_i++];

            throw new ParseError(msg, T.Line, T.Column);
        }

        private Token Previous() => _tokens[_i - 1];

        public ProgramNode ParseProgram()
        {
            var stmts = new List<Stmt>();

            while (!Check(TokenType.EOF))
            {
                try
                {
                    var stmt = ParseStatement();
                    if (stmt != null)
                        stmts.Add(stmt);
                }
                catch (ParseError ex)
                {
                    Errors.Add(new ParseDiagnostic(ex.Line, ex.Column, ex.Message));
                    Synchronize();
                }
            }

            return new ProgramNode(stmts);
        }
        private void Synchronize()
        {
            if (!Check(TokenType.EOF))
                _i++;

            while (!Check(TokenType.EOF))
            {
                if (Previous().Type == TokenType.Semicolon)
                    return;

                if (Check(TokenType.RBrace))
                    return;

                switch (T.Type)
                {
                    case TokenType.Var:
                    case TokenType.Let:
                    case TokenType.Const:
                    case TokenType.Print:
                    case TokenType.If:
                    case TokenType.Else:
                    case TokenType.While:
                    case TokenType.For:
                    case TokenType.Switch:
                    case TokenType.Function:
                    case TokenType.Return:
                    case TokenType.Break:
                    case TokenType.Continue:
                    case TokenType.LBrace:
                        return;
                }

                _i++;
            }
        }

        private Stmt ParseStatement()
        {
            if (Match(TokenType.Var)) return ParseVarDecl("var");
            if (Match(TokenType.Let)) return ParseVarDecl("let");
            if (Match(TokenType.Const)) return ParseVarDecl("const");
            if (Match(TokenType.Print)) return ParsePrint();
            if (Match(TokenType.If)) return ParseIf();
            if (Match(TokenType.While)) return ParseWhile();
            if (Match(TokenType.For)) return ParseFor();
            if (Match(TokenType.Switch)) return ParseSwitch();
            if (Match(TokenType.Function)) return ParseFunction();
            if (Match(TokenType.Return)) return ParseReturn();
            if (Match(TokenType.Break))
            {
                var tok = Previous();
                Expect(TokenType.Semicolon, "Se esperaba ';' después de break.");
                return new BreakStmt(tok.Line, tok.Column);
            }
            if (Match(TokenType.Continue))
            {
                var tok = Previous();
                Expect(TokenType.Semicolon, "Se esperaba ';' después de continue.");
                return new ContinueStmt(tok.Line, tok.Column);
            }
            if (Match(TokenType.LBrace))
            {
                var lbrace = Previous();
                return ParseBlock(lbrace.Line, lbrace.Column);
            }

            if (Check(TokenType.Identifier) && IsAssignmentOperator(PeekType(1)))
                return ParseAssignmentStatement();

            return ParseExpressionStatement();
        }

        private Stmt ParseVarDecl(string kind)
        {
            var kindTok = Previous();
            var id = Expect(TokenType.Identifier, "Se esperaba nombre de variable.");
            Expr? init = null;

            if (Match(TokenType.Assign))
                init = ParseExpression();

            Expect(TokenType.Semicolon, "Se esperaba ';' al final de la declaración.");
            return new VarDecl(kind, id.Lexeme, init, kindTok.Line, kindTok.Column);
        }

        private Stmt ParsePrint()
        {
            var p = Previous();
            Expect(TokenType.LParen, "Se esperaba '(' después de print.");
            var expr = ParseExpression();
            Expect(TokenType.RParen, "Se esperaba ')' después de la expresión.");
            Expect(TokenType.Semicolon, "Se esperaba ';' después de print.");
            return new PrintStmt(expr, p.Line, p.Column);
        }

        private Stmt ParseIf()
        {
            var tok = Previous();
            Expect(TokenType.LParen, "Se esperaba '(' después de if.");
            var condition = ParseExpression();
            Expect(TokenType.RParen, "Se esperaba ')' después de la condición.");

            var thenBranch = ParseStatement();
            Stmt? elseBranch = null;

            if (Match(TokenType.Else))
                elseBranch = ParseStatement();

            return new IfStmt(condition, thenBranch, elseBranch, tok.Line, tok.Column);
        }

        private Stmt ParseWhile()
        {
            var tok = Previous();
            Expect(TokenType.LParen, "Se esperaba '(' después de while.");
            var condition = ParseExpression();
            Expect(TokenType.RParen, "Se esperaba ')' después de la condición.");

            var body = ParseStatement();
            return new WhileStmt(condition, body, tok.Line, tok.Column);
        }

        private Stmt ParseFor()
        {
            var tok = Previous();
            Expect(TokenType.LParen, "Se esperaba '(' después de for.");

            Stmt? init = null;
            if (!Check(TokenType.Semicolon))
            {
                if (Match(TokenType.Var)) init = ParseVarDeclNoConsumeKind("var");
                else if (Match(TokenType.Let)) init = ParseVarDeclNoConsumeKind("let");
                else if (Match(TokenType.Const)) init = ParseVarDeclNoConsumeKind("const");
                else init = ParseAssignmentOrExprNoSemicolon();
            }
            Expect(TokenType.Semicolon, "Se esperaba ';' en la inicialización del for.");

            Expr? condition = null;
            if (!Check(TokenType.Semicolon))
                condition = ParseExpression();
            Expect(TokenType.Semicolon, "Se esperaba ';' en la condición del for.");

            Stmt? increment = null;
            if (!Check(TokenType.RParen))
                increment = ParseAssignmentOrExprNoSemicolon();

            Expect(TokenType.RParen, "Se esperaba ')' al final del for.");

            var body = ParseStatement();
            return new ForStmt(init, condition, increment, body, tok.Line, tok.Column);
        }

        private Stmt ParseSwitch()
        {
            var tok = Previous();
            Expect(TokenType.LParen, "Se esperaba '(' después de switch.");
            var expr = ParseExpression();
            Expect(TokenType.RParen, "Se esperaba ')' después de la expresión.");
            Expect(TokenType.LBrace, "Se esperaba '{' en switch.");

            var casesList = new List<SwitchCase>();
            List<Stmt>? defaultStatements = null;

            while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
            {
                if (Match(TokenType.Case))
                {
                    var value = ParseExpression();
                    Expect(TokenType.Colon, "Se esperaba ':' después de case.");

                    var stmts = new List<Stmt>();
                    while (!Check(TokenType.Case) && !Check(TokenType.Default) && !Check(TokenType.RBrace))
                    {
                        stmts.Add(ParseStatement());
                    }

                    casesList.Add(new SwitchCase(value, stmts));
                }
                else if (Match(TokenType.Default))
                {
                    Expect(TokenType.Colon, "Se esperaba ':' después de default.");
                    defaultStatements = new List<Stmt>();

                    while (!Check(TokenType.Case) && !Check(TokenType.RBrace))
                    {
                        defaultStatements.Add(ParseStatement());
                    }
                }
                else
                {
                    throw new ParseError("Se esperaba 'case' o 'default' dentro de switch.", T.Line, T.Column);
                }
            }

            Expect(TokenType.RBrace, "Se esperaba '}' al final de switch.");
            return new SwitchStmt(expr, casesList, defaultStatements, tok.Line, tok.Column);
        }

        private Stmt ParseFunction()
        {
            var tok = Previous();
            var name = Expect(TokenType.Identifier, "Se esperaba nombre de función.");

            Expect(TokenType.LParen, "Se esperaba '(' en la función.");
            var parameters = new List<string>();

            if (!Check(TokenType.RParen))
            {
                do
                {
                    var param = Expect(TokenType.Identifier, "Se esperaba nombre de parámetro.");
                    parameters.Add(param.Lexeme);
                }
                while (Match(TokenType.Comma));
            }

            Expect(TokenType.RParen, "Se esperaba ')' después de parámetros.");
            Expect(TokenType.LBrace, "Se esperaba '{' en el cuerpo de la función.");

            var body = ParseBlock(tok.Line, tok.Column);
            return new FunctionDecl(name.Lexeme, parameters, body, tok.Line, tok.Column);
        }

        private Stmt ParseReturn()
        {
            var tok = Previous();

            if (Match(TokenType.Semicolon))
                return new ReturnStmt(null, tok.Line, tok.Column);

            var expr = ParseExpression();
            Expect(TokenType.Semicolon, "Se esperaba ';' después de return.");
            return new ReturnStmt(expr, tok.Line, tok.Column);
        }

        private BlockStmt ParseBlock(int line, int col)
        {
            var statements = new List<Stmt>();

            while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))
            {
                statements.Add(ParseStatement());
            }

            Expect(TokenType.RBrace, "Se esperaba '}' al final del bloque.");
            return new BlockStmt(statements, line, col);
        }

        private Stmt ParseAssignmentStatement()
        {
            var id = Expect(TokenType.Identifier, "Se esperaba identificador.");
            var op = T.Type;
            _i++;

            Expr expr = ParseExpression();

            if (op == TokenType.PlusAssign)
                expr = new BinaryExpr(new IdentifierExpr(id.Lexeme, id.Line, id.Column), BinaryOp.Add, expr, id.Line, id.Column);
            else if (op == TokenType.MinusAssign)
                expr = new BinaryExpr(new IdentifierExpr(id.Lexeme, id.Line, id.Column), BinaryOp.Sub, expr, id.Line, id.Column);
            else if (op == TokenType.StarAssign)
                expr = new BinaryExpr(new IdentifierExpr(id.Lexeme, id.Line, id.Column), BinaryOp.Mul, expr, id.Line, id.Column);
            else if (op == TokenType.SlashAssign)
                expr = new BinaryExpr(new IdentifierExpr(id.Lexeme, id.Line, id.Column), BinaryOp.Div, expr, id.Line, id.Column);

            Expect(TokenType.Semicolon, "Se esperaba ';' al final de la asignación.");
            return new AssignStmt(id.Lexeme, expr, id.Line, id.Column);
        }

        private Stmt ParseExpressionStatement()
        {
            var expr = ParseExpression();
            Expect(TokenType.Semicolon, "Se esperaba ';' al final de la expresión.");
            return new ExprStmt(expr, expr.Line, expr.Column);
        }

        private Stmt ParseVarDeclNoConsumeKind(string kind)
        {
            var tok = Previous();
            var id = Expect(TokenType.Identifier, "Se esperaba identificador.");
            Expr? init = null;

            if (Match(TokenType.Assign))
                init = ParseExpression();

            return new VarDecl(kind, id.Lexeme, init, tok.Line, tok.Column);
        }

        private Stmt ParseAssignmentOrExprNoSemicolon()
        {
            if (Check(TokenType.Identifier) && IsAssignmentOperator(PeekType(1)))
            {
                var id = Expect(TokenType.Identifier, "Se esperaba identificador.");
                var op = T.Type;
                _i++;

                Expr expr;
                if (op == TokenType.Increment)
                {
                    expr = new BinaryExpr(new IdentifierExpr(id.Lexeme, id.Line, id.Column), BinaryOp.Add, new NumberExpr(1, id.Line, id.Column), id.Line, id.Column);
                    return new AssignStmt(id.Lexeme, expr, id.Line, id.Column);
                }
                if (op == TokenType.Decrement)
                {
                    expr = new BinaryExpr(new IdentifierExpr(id.Lexeme, id.Line, id.Column), BinaryOp.Sub, new NumberExpr(1, id.Line, id.Column), id.Line, id.Column);
                    return new AssignStmt(id.Lexeme, expr, id.Line, id.Column);
                }

                expr = ParseExpression();

                if (op == TokenType.PlusAssign)
                    expr = new BinaryExpr(new IdentifierExpr(id.Lexeme, id.Line, id.Column), BinaryOp.Add, expr, id.Line, id.Column);
                else if (op == TokenType.MinusAssign)
                    expr = new BinaryExpr(new IdentifierExpr(id.Lexeme, id.Line, id.Column), BinaryOp.Sub, expr, id.Line, id.Column);
                else if (op == TokenType.StarAssign)
                    expr = new BinaryExpr(new IdentifierExpr(id.Lexeme, id.Line, id.Column), BinaryOp.Mul, expr, id.Line, id.Column);
                else if (op == TokenType.SlashAssign)
                    expr = new BinaryExpr(new IdentifierExpr(id.Lexeme, id.Line, id.Column), BinaryOp.Div, expr, id.Line, id.Column);

                return new AssignStmt(id.Lexeme, expr, id.Line, id.Column);
            }

            var e = ParseExpression();
            return new ExprStmt(e, e.Line, e.Column);
        }

        private bool IsAssignmentOperator(TokenType type)
        {
            return type == TokenType.Assign
                || type == TokenType.PlusAssign
                || type == TokenType.MinusAssign
                || type == TokenType.StarAssign
                || type == TokenType.SlashAssign
                || type == TokenType.Increment
                || type == TokenType.Decrement;
        }

        private TokenType PeekType(int k)
        {
            int j = _i + k;
            return j < _tokens.Count ? _tokens[j].Type : TokenType.EOF;
        }

        // =========================
        // EXPRESIONES CON PRECEDENCIA
        // =========================

        private Expr ParseExpression() => ParseOr();

        private Expr ParseOr()
        {
            var left = ParseAnd();

            while (Match(TokenType.OrOr))
            {
                var op = Previous();
                var right = ParseAnd();
                left = new BinaryExpr(left, BinaryOp.Or, right, op.Line, op.Column);
            }

            return left;
        }

        private Expr ParseAnd()
        {
            var left = ParseEquality();

            while (Match(TokenType.AndAnd))
            {
                var op = Previous();
                var right = ParseEquality();
                left = new BinaryExpr(left, BinaryOp.And, right, op.Line, op.Column);
            }

            return left;
        }

        private Expr ParseEquality()
        {
            var left = ParseComparison();

            while (true)
            {
                if (Match(TokenType.EqualEqual))
                {
                    var op = Previous();
                    var right = ParseComparison();
                    left = new BinaryExpr(left, BinaryOp.Equal, right, op.Line, op.Column);
                }
                else if (Match(TokenType.BangEqual))
                {
                    var op = Previous();
                    var right = ParseComparison();
                    left = new BinaryExpr(left, BinaryOp.NotEqual, right, op.Line, op.Column);
                }
                else
                    break;
            }

            return left;
        }

        private Expr ParseComparison()
        {
            var left = ParseTerm();

            while (true)
            {
                if (Match(TokenType.Less))
                {
                    var op = Previous();
                    left = new BinaryExpr(left, BinaryOp.Less, ParseTerm(), op.Line, op.Column);
                }
                else if (Match(TokenType.LessEqual))
                {
                    var op = Previous();
                    left = new BinaryExpr(left, BinaryOp.LessEqual, ParseTerm(), op.Line, op.Column);
                }
                else if (Match(TokenType.Greater))
                {
                    var op = Previous();
                    left = new BinaryExpr(left, BinaryOp.Greater, ParseTerm(), op.Line, op.Column);
                }
                else if (Match(TokenType.GreaterEqual))
                {
                    var op = Previous();
                    left = new BinaryExpr(left, BinaryOp.GreaterEqual, ParseTerm(), op.Line, op.Column);
                }
                else
                    break;
            }

            return left;
        }

        private Expr ParseTerm()
        {
            var left = ParseFactor();

            while (true)
            {
                if (Match(TokenType.Plus))
                {
                    var op = Previous();
                    left = new BinaryExpr(left, BinaryOp.Add, ParseFactor(), op.Line, op.Column);
                }
                else if (Match(TokenType.Minus))
                {
                    var op = Previous();
                    left = new BinaryExpr(left, BinaryOp.Sub, ParseFactor(), op.Line, op.Column);
                }
                else
                    break;
            }

            return left;
        }

        private Expr ParseFactor()
        {
            var left = ParseUnary();

            while (true)
            {
                if (Match(TokenType.Star))
                {
                    var op = Previous();
                    left = new BinaryExpr(left, BinaryOp.Mul, ParseUnary(), op.Line, op.Column);
                }
                else if (Match(TokenType.Slash))
                {
                    var op = Previous();
                    left = new BinaryExpr(left, BinaryOp.Div, ParseUnary(), op.Line, op.Column);
                }
                else if (Match(TokenType.Percent))
                {
                    var op = Previous();
                    left = new BinaryExpr(left, BinaryOp.Mod, ParseUnary(), op.Line, op.Column);
                }
                else
                    break;
            }

            return left;
        }

        private Expr ParseUnary()
        {
            if (Match(TokenType.Plus))
            {
                var op = Previous();
                return new UnaryExpr(UnaryOp.Plus, ParseUnary(), op.Line, op.Column);
            }

            if (Match(TokenType.Minus))
            {
                var op = Previous();
                return new UnaryExpr(UnaryOp.Minus, ParseUnary(), op.Line, op.Column);
            }

            if (Match(TokenType.Bang))
            {
                var op = Previous();
                return new UnaryExpr(UnaryOp.Not, ParseUnary(), op.Line, op.Column);
            }

            return ParsePrimary();
        }

        private Expr ParsePrimary()
        {
            if (Match(TokenType.Number))
            {
                var num = Previous();
                double v = double.Parse(num.Lexeme, CultureInfo.InvariantCulture);
                return new NumberExpr(v, num.Line, num.Column);
            }

            if (Match(TokenType.String))
            {
                var s = Previous();
                string value = s.Lexeme.Length >= 2 ? s.Lexeme.Substring(1, s.Lexeme.Length - 2) : "";
                return new StringExpr(value, s.Line, s.Column);
            }

            if (Match(TokenType.True))
            {
                var t = Previous();
                return new BoolExpr(true, t.Line, t.Column);
            }

            if (Match(TokenType.False))
            {
                var t = Previous();
                return new BoolExpr(false, t.Line, t.Column);
            }

            if (Match(TokenType.Identifier))
            {
                var id = Previous();

                if (Match(TokenType.LParen))
                {
                    var args = new List<Expr>();

                    if (!Check(TokenType.RParen))
                    {
                        do
                        {
                            args.Add(ParseExpression());
                        }
                        while (Match(TokenType.Comma));
                    }

                    Expect(TokenType.RParen, "Se esperaba ')' en la llamada a función.");
                    return new CallExpr(id.Lexeme, args, id.Line, id.Column);
                }

                return new IdentifierExpr(id.Lexeme, id.Line, id.Column);
            }

            if (Match(TokenType.LParen))
            {
                var expr = ParseExpression();
                Expect(TokenType.RParen, "Se esperaba ')'.");
                return expr;
            }

            throw new ParseError($"Token inesperado: {T.Type} '{T.Lexeme}'", T.Line, T.Column);
        }

        public sealed class ParseDiagnostic
        {
            public int Line { get; }
            public int Column { get; }
            public string Message { get; }

            public ParseDiagnostic(int line, int column, string message)
            {
                Line = line;
                Column = column;
                Message = message;
            }

            public override string ToString() => $"[L{Line},C{Column}] {Message}";
        }

    }
}
