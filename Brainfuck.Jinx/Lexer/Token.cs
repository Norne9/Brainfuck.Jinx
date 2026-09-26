namespace Brainfuck.Jinx.Lexer;

/// <summary>
/// One of Brainfuck's eight instructions, produced by
/// <see cref="ILexer.ParseTokens"/>.
/// </summary>
/// <remarks>
/// The lexer reduces the source text to this small alphabet. Every other
/// character is treated as a comment and discarded, which is how Brainfuck's
/// "any non-instruction character is ignored" rule is implemented.
/// </remarks>
public enum Token
{
    /// <summary>The <c>&lt;</c> instruction: move the pointer left.</summary>
    ShiftLeft,

    /// <summary>The <c>&gt;</c> instruction: move the pointer right.</summary>
    ShiftRight,

    /// <summary>The <c>+</c> instruction: increment the current cell.</summary>
    Increment,

    /// <summary>The <c>-</c> instruction: decrement the current cell.</summary>
    Decrement,

    /// <summary>The <c>[</c> instruction: begin a loop.</summary>
    LeftBracket,

    /// <summary>The <c>]</c> instruction: end a loop.</summary>
    RightBracket,

    /// <summary>The <c>.</c> instruction: write the current cell.</summary>
    Write,

    /// <summary>The <c>,</c> instruction: read into the current cell.</summary>
    Read,
}
