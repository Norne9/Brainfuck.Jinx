namespace Brainfuck.Jinx.Lexer;

/// <summary>
/// Converts Brainfuck source text into the eight-instruction
/// <see cref="Token"/> alphabet.
/// </summary>
public interface ILexer
{
    /// <summary>
    /// Scans <paramref name="input"/> and returns the instruction stream,
    /// ignoring every character that is not a Brainfuck instruction.
    /// </summary>
    /// <param name="input">The Brainfuck source text.</param>
    /// <returns>The tokens in source order.</returns>
    public IReadOnlyList<Token> ParseTokens(ReadOnlySpan<char> input);
}
