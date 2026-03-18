using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SemanticamenteElocuente
{
    public static class Highlighter
    {
        public static void Colorize(RichTextBox rtb, IEnumerable<Token> tokens)
        {
            Colorize(rtb, tokens, (IEnumerable<(int line, int col, string msg, bool isWarning)>?)null);
        }

        public static void Colorize(
            RichTextBox rtb,
            IEnumerable<Token> tokens,
            IEnumerable<(int line, int col, string msg)>? diagnostics)
        {
            var mapped = diagnostics?.Select(d => (d.line, d.col, d.msg, false));
            Colorize(rtb, tokens, mapped);
        }

        public static void Colorize(
            RichTextBox rtb,
            IEnumerable<Token> tokens,
            IEnumerable<(int line, int col, string msg, bool isWarning)>? diagnostics)
        {
            if (rtb is null)
                return;

            int oldStart = rtb.SelectionStart;
            int oldLength = rtb.SelectionLength;

            rtb.SuspendLayout();

            try
            {
                rtb.SelectAll();
                rtb.SelectionColor = Color.Black;
                rtb.SelectionBackColor = Color.White;
                rtb.SelectionFont = new Font(rtb.Font, FontStyle.Regular);

                foreach (var t in tokens)
                {
                    if (string.IsNullOrEmpty(t.Lexeme))
                        continue;

                    int start = IndexFromLineCol(rtb, t.Line, t.Column);
                    if (start < 0 || start >= rtb.TextLength)
                        continue;

                    int length = Math.Min(t.Lexeme.Length, rtb.TextLength - start);
                    if (length <= 0)
                        continue;

                    rtb.Select(start, length);
                    rtb.SelectionColor = GetTokenColor(t.Type);
                    rtb.SelectionBackColor = Color.White;
                    rtb.SelectionFont = new Font(rtb.Font, GetTokenStyle(t.Type));
                }

                if (diagnostics != null)
                {
                    foreach (var d in diagnostics)
                    {
                        int start = IndexFromLineCol(rtb, d.line, d.col);
                        if (start < 0 || start >= rtb.TextLength)
                            continue;

                        int length = 1;
                        if (start + length > rtb.TextLength)
                            length = rtb.TextLength - start;

                        rtb.Select(start, length);

                        if (d.isWarning)
                        {
                            rtb.SelectionBackColor = Color.FromArgb(255, 247, 204);
                            rtb.SelectionColor = Color.DarkOrange;
                            rtb.SelectionFont = new Font(rtb.Font, FontStyle.Underline | FontStyle.Bold);
                        }
                        else
                        {
                            rtb.SelectionBackColor = Color.MistyRose;
                            rtb.SelectionColor = Color.DarkRed;
                            rtb.SelectionFont = new Font(rtb.Font, FontStyle.Bold);
                        }
                    }
                }
            }
            finally
            {
                int safeStart = Math.Max(0, Math.Min(oldStart, rtb.TextLength));
                int safeLength = Math.Max(0, Math.Min(oldLength, rtb.TextLength - safeStart));

                rtb.Select(safeStart, safeLength);
                rtb.ResumeLayout();
            }
        }

        private static Color GetTokenColor(TokenType tt) => tt switch
        {
            TokenType.Number => Color.DarkBlue,
            TokenType.String => Color.Brown,
            TokenType.Identifier => Color.DarkGreen,

            TokenType.Var or TokenType.Let or TokenType.Const or
            TokenType.Print or TokenType.If or TokenType.Else or
            TokenType.While or TokenType.For or TokenType.Switch or
            TokenType.Case or TokenType.Default or TokenType.Break or
            TokenType.Continue or TokenType.Function or TokenType.Return or
            TokenType.True or TokenType.False
                => Color.MediumVioletRed,

            TokenType.Plus or TokenType.Minus or TokenType.Star or TokenType.Slash or
            TokenType.Percent or TokenType.EqualEqual or TokenType.BangEqual or
            TokenType.Less or TokenType.LessEqual or TokenType.Greater or
            TokenType.GreaterEqual or TokenType.AndAnd or TokenType.OrOr or
            TokenType.Bang or TokenType.Increment or TokenType.Decrement
                => Color.Firebrick,

            TokenType.Assign or TokenType.PlusAssign or TokenType.MinusAssign or
            TokenType.StarAssign or TokenType.SlashAssign
                => Color.Sienna,

            TokenType.Semicolon or TokenType.Comma or TokenType.Colon or TokenType.Dot
                => Color.SlateGray,

            TokenType.LParen or TokenType.RParen or TokenType.LBrace or TokenType.RBrace
                => Color.SteelBlue,

            TokenType.Comment => Color.Gray,
            TokenType.Unknown => Color.Red,
            _ => Color.Black
        };

        private static FontStyle GetTokenStyle(TokenType tt) => tt switch
        {
            TokenType.Comment => FontStyle.Italic,

            TokenType.Var or TokenType.Let or TokenType.Const or
            TokenType.Print or TokenType.If or TokenType.Else or
            TokenType.While or TokenType.For or TokenType.Switch or
            TokenType.Case or TokenType.Default or TokenType.Break or
            TokenType.Continue or TokenType.Function or TokenType.Return or
            TokenType.True or TokenType.False
                => FontStyle.Bold,

            _ => FontStyle.Regular
        };

        private static int IndexFromLineCol(RichTextBox rtb, int line, int col)
        {
            if (line <= 0 || col <= 0)
                return -1;

            int lineIndex = line - 1;
            if (lineIndex >= rtb.Lines.Length)
                return -1;

            int baseIndex = rtb.GetFirstCharIndexFromLine(lineIndex);
            if (baseIndex < 0)
                return -1;

            return baseIndex + (col - 1);
        }
    }
}