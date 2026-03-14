using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticamenteElocuente
{
    public sealed class Lexer
    {
        private readonly string _src;
        private int _pos;
        private int _line = 1;
        private int _col = 1;

        private static readonly Dictionary<string, TokenType> _keywords = new(StringComparer.Ordinal)
        {
            ["var"] = TokenType.Var,
            ["let"] = TokenType.Let,
            ["const"] = TokenType.Const,
            ["print"] = TokenType.Print,
            ["if"] = TokenType.If,
            ["else"] = TokenType.Else,
            ["while"] = TokenType.While,
            ["for"] = TokenType.For,
            ["switch"] = TokenType.Switch,
            ["case"] = TokenType.Case,
            ["default"] = TokenType.Default,
            ["break"] = TokenType.Break,
            ["continue"] = TokenType.Continue,
            ["function"] = TokenType.Function,
            ["return"] = TokenType.Return,
            ["true"] = TokenType.True,
            ["false"] = TokenType.False
        };

        public Lexer(string source)
        {
            _src = source ?? string.Empty;
        }

        private char Current => _pos < _src.Length ? _src[_pos] : '\0';

        private char Peek(int k = 1)
        {
            int i = _pos + k;
            return i < _src.Length ? _src[i] : '\0';
        }

        private void Advance(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                if (_pos >= _src.Length) return;

                if (_src[_pos] == '\n')
                {
                    _line++;
                    _col = 1;
                }
                else
                {
                    _col++;
                }

                _pos++;
            }
        }

        private Token Make(TokenType type, string lexeme, int line, int col)
            => new Token(type, lexeme, line, col);

        public IEnumerable<Token> ScanAll(bool includeTrivia = false)
        {
            while (true)
            {
                var t = NextToken();

                if (includeTrivia || (t.Type != TokenType.Whitespace && t.Type != TokenType.Comment))
                    yield return t;

                if (t.Type == TokenType.EOF)
                    yield break;
            }
        }

        public Token NextToken()
        {
            // Whitespace
            if (char.IsWhiteSpace(Current))
            {
                int line0 = _line, col0 = _col;
                var sb = new StringBuilder();

                while (char.IsWhiteSpace(Current))
                {
                    sb.Append(Current);
                    Advance();
                }

                return Make(TokenType.Whitespace, sb.ToString(), line0, col0);
            }

            // Comentario de línea //
            if (Current == '/' && Peek() == '/')
            {
                int line0 = _line, col0 = _col;
                var sb = new StringBuilder();

                while (Current != '\n' && Current != '\0')
                {
                    sb.Append(Current);
                    Advance();
                }

                return Make(TokenType.Comment, sb.ToString(), line0, col0);
            }

            // Comentario de bloque /* ... */
            if (Current == '/' && Peek() == '*')
            {
                int line0 = _line, col0 = _col;
                var sb = new StringBuilder();

                sb.Append(Current); Advance();
                sb.Append(Current); Advance();

                while (!(Current == '*' && Peek() == '/') && Current != '\0')
                {
                    sb.Append(Current);
                    Advance();
                }

                if (Current == '*' && Peek() == '/')
                {
                    sb.Append(Current); Advance();
                    sb.Append(Current); Advance();
                }

                return Make(TokenType.Comment, sb.ToString(), line0, col0);
            }

            // EOF
            if (Current == '\0')
                return Make(TokenType.EOF, string.Empty, _line, _col);

            // Number
            if (char.IsDigit(Current))
            {
                int line0 = _line, col0 = _col;
                var sb = new StringBuilder();

                while (char.IsDigit(Current))
                {
                    sb.Append(Current);
                    Advance();
                }

                if (Current == '.' && char.IsDigit(Peek()))
                {
                    sb.Append(Current);
                    Advance();

                    while (char.IsDigit(Current))
                    {
                        sb.Append(Current);
                        Advance();
                    }
                }

                return Make(TokenType.Number, sb.ToString(), line0, col0);
            }

            // String
            if (Current == '"')
            {
                int line0 = _line, col0 = _col;
                var sb = new StringBuilder();

                sb.Append(Current);
                Advance();

                while (Current != '"' && Current != '\0')
                {
                    if (Current == '\\' && Peek() != '\0')
                    {
                        sb.Append(Current);
                        Advance();
                    }

                    sb.Append(Current);
                    Advance();
                }

                if (Current == '"')
                {
                    sb.Append(Current);
                    Advance();
                }

                return Make(TokenType.String, sb.ToString(), line0, col0);
            }

            // Identifier / keyword
            if (char.IsLetter(Current) || Current == '_')
            {
                int line0 = _line, col0 = _col;
                var sb = new StringBuilder();

                sb.Append(Current);
                Advance();

                while (char.IsLetterOrDigit(Current) || Current == '_')
                {
                    sb.Append(Current);
                    Advance();
                }

                string lex = sb.ToString();

                if (_keywords.TryGetValue(lex, out var kw))
                    return Make(kw, lex, line0, col0);

                return Make(TokenType.Identifier, lex, line0, col0);
            }

            // Operadores dobles primero
            {
                int line0 = _line, col0 = _col;

                if (Current == '+' && Peek() == '+') { Advance(2); return Make(TokenType.Increment, "++", line0, col0); }
                if (Current == '-' && Peek() == '-') { Advance(2); return Make(TokenType.Decrement, "--", line0, col0); }

                if (Current == '+' && Peek() == '=') { Advance(2); return Make(TokenType.PlusAssign, "+=", line0, col0); }
                if (Current == '-' && Peek() == '=') { Advance(2); return Make(TokenType.MinusAssign, "-=", line0, col0); }
                if (Current == '*' && Peek() == '=') { Advance(2); return Make(TokenType.StarAssign, "*=", line0, col0); }
                if (Current == '/' && Peek() == '=') { Advance(2); return Make(TokenType.SlashAssign, "/=", line0, col0); }

                if (Current == '=' && Peek() == '=') { Advance(2); return Make(TokenType.EqualEqual, "==", line0, col0); }
                if (Current == '!' && Peek() == '=') { Advance(2); return Make(TokenType.BangEqual, "!=", line0, col0); }

                if (Current == '<' && Peek() == '=') { Advance(2); return Make(TokenType.LessEqual, "<=", line0, col0); }
                if (Current == '>' && Peek() == '=') { Advance(2); return Make(TokenType.GreaterEqual, ">=", line0, col0); }

                if (Current == '&' && Peek() == '&') { Advance(2); return Make(TokenType.AndAnd, "&&", line0, col0); }
                if (Current == '|' && Peek() == '|') { Advance(2); return Make(TokenType.OrOr, "||", line0, col0); }
            }

            // Operadores simples y separadores
            {
                int line0 = _line, col0 = _col;
                char ch = Current;
                Advance();

                return ch switch
                {
                    '+' => Make(TokenType.Plus, "+", line0, col0),
                    '-' => Make(TokenType.Minus, "-", line0, col0),
                    '*' => Make(TokenType.Star, "*", line0, col0),
                    '/' => Make(TokenType.Slash, "/", line0, col0),
                    '%' => Make(TokenType.Percent, "%", line0, col0),

                    '=' => Make(TokenType.Assign, "=", line0, col0),
                    '!' => Make(TokenType.Bang, "!", line0, col0),
                    '<' => Make(TokenType.Less, "<", line0, col0),
                    '>' => Make(TokenType.Greater, ">", line0, col0),

                    ';' => Make(TokenType.Semicolon, ";", line0, col0),
                    ',' => Make(TokenType.Comma, ",", line0, col0),
                    ':' => Make(TokenType.Colon, ":", line0, col0),
                    '.' => Make(TokenType.Dot, ".", line0, col0),

                    '(' => Make(TokenType.LParen, "(", line0, col0),
                    ')' => Make(TokenType.RParen, ")", line0, col0),
                    '{' => Make(TokenType.LBrace, "{", line0, col0),
                    '}' => Make(TokenType.RBrace, "}", line0, col0),

                    _ => Make(TokenType.Unknown, ch.ToString(), line0, col0)
                };
            }
        }
    }
}
