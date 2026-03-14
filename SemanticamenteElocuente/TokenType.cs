using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticamenteElocuente
{
    public enum TokenType
    {
        // Literales / identificadores
        Number,
        Identifier,
        String,

        // Keywords
        Var,
        Let,
        Const,
        Print,
        If,
        Else,
        While,
        For,
        Switch,
        Case,
        Default,
        Break,
        Continue,
        Function,
        Return,
        True,
        False,

        // Operadores aritméticos
        Plus,           // +
        Minus,          // -
        Star,           // *
        Slash,          // /
        Percent,        // %

        Increment,      // ++
        Decrement,      // --

        Assign,         // =
        PlusAssign,     // +=
        MinusAssign,    // -=
        StarAssign,     // *=
        SlashAssign,    // /=

        // Comparación
        EqualEqual,     // ==
        BangEqual,      // !=
        Less,           // <
        LessEqual,      // <=
        Greater,        // >
        GreaterEqual,   // >=

        // Lógicos
        AndAnd,         // &&
        OrOr,           // ||
        Bang,           // !

        // Separadores
        Semicolon,      // ;
        Comma,          // ,
        Colon,          // :
        Dot,            // .

        LParen,         // (
        RParen,         // )
        LBrace,         // {
        RBrace,         // }

        // Trivia
        Whitespace,
        Comment,

        // Control
        Unknown,
        EOF
    }
}
