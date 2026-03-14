using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticamenteElocuente
{
    public static class Highlighter
    {
        public static void Colorize(
            RichTextBox rtb,
            IEnumerable<Token> tokens,
            IEnumerable<(int line, int col, string msg)>? errors = null)
        {
            if (rtb is null) return;

            int caretStart = rtb.SelectionStart;
            int caretLength = rtb.SelectionLength;

            rtb.SuspendLayout();

            try
            {
                // Reset global
                rtb.SelectAll();
                rtb.SelectionColor = Color.Black;
                rtb.SelectionBackColor = Color.White;
                rtb.SelectionFont = new Font(rtb.Font, FontStyle.Regular);

                foreach (var t in tokens)
                {
                    if (string.IsNullOrEmpty(t.Lexeme))
                        continue;

                    int idx = IndexFromLineCol(rtb, t.Line, t.Column);
                    if (idx < 0 || idx >= rtb.TextLength)
                        continue;

                    int safeLen = Math.Min(t.Lexeme.Length, rtb.TextLength - idx);
                    if (safeLen <= 0)
                        continue;

                    rtb.Select(idx, safeLen);
                    rtb.SelectionColor = GetColor(t);
                    rtb.SelectionBackColor = Color.White;

                    var style = GetFontStyle(t);
                    if (style != FontStyle.Regular)
                        rtb.SelectionFont = new Font(rtb.Font, style);
                    else
                        rtb.SelectionFont = new Font(rtb.Font, FontStyle.Regular);
                }

                if (errors is not null)
                {
                    foreach (var e in errors)
                    {
                        int idx = IndexFromLineCol(rtb, e.line, e.col);
                        if (idx < 0 || idx >= rtb.TextLength)
                            continue;

                        rtb.Select(idx, 1);
                        rtb.SelectionBackColor = Color.MistyRose;
                        rtb.SelectionColor = Color.DarkRed;
                        rtb.SelectionFont = new Font(rtb.Font, FontStyle.Bold);
                    }
                }
            }
            finally
            {
                int restoreStart = Math.Max(0, Math.Min(caretStart, rtb.TextLength));
                int restoreLen = Math.Max(0, Math.Min(caretLength, rtb.TextLength - restoreStart));
                rtb.Select(restoreStart, restoreLen);
                rtb.ResumeLayout();
            }
        }

        private static Color GetColor(Token t) => t.Type switch
        {
            TokenType.Number => Color.DarkBlue,
            TokenType.String => Color.Brown,
            TokenType.Identifier => Color.DarkGreen,

            TokenType.Var or TokenType.Let or TokenType.Const or
            TokenType.Print or TokenType.If or TokenType.Else or
            TokenType.While or TokenType.For or
            TokenType.Switch or TokenType.Case or TokenType.Default or
            TokenType.Break or TokenType.Continue or
            TokenType.Function or TokenType.Return or
            TokenType.True or TokenType.False
                => Color.MediumVioletRed,

            TokenType.Plus or TokenType.Minus or TokenType.Star or
            TokenType.Slash or TokenType.Percent or
            TokenType.EqualEqual or TokenType.BangEqual or
            TokenType.Less or TokenType.LessEqual or
            TokenType.Greater or TokenType.GreaterEqual or
            TokenType.AndAnd or TokenType.OrOr or TokenType.Bang or
            TokenType.Increment or TokenType.Decrement
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

        private static FontStyle GetFontStyle(Token t) => t.Type switch
        {
            TokenType.Comment => FontStyle.Italic,
            TokenType.Var or TokenType.Let or TokenType.Const or
            TokenType.Print or TokenType.If or TokenType.Else or
            TokenType.While or TokenType.For or
            TokenType.Switch or TokenType.Case or TokenType.Default or
            TokenType.Break or TokenType.Continue or
            TokenType.Function or TokenType.Return
                => FontStyle.Bold,
            _ => FontStyle.Regular
        };

        private static int IndexFromLineCol(RichTextBox rtb, int line, int col)
        {
            if (line <= 0 || col <= 0)
                return -1;

            int lineIdx = line - 1;
            if (lineIdx >= rtb.Lines.Length)
                return -1;

            int baseIdx = rtb.GetFirstCharIndexFromLine(lineIdx);
            if (baseIdx < 0)
                return -1;

            return baseIdx + (col - 1);
        }
    }
}
