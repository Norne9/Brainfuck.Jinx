namespace Brainfuck.Jinx.Lexer;

/// <summary>
/// The default <see cref="ILexer"/>: a single-pass scanner over the raw source
/// text.
/// </summary>
/// <remarks>
/// <para>
/// The scanner tracks the line and column so bracket errors can point at the
/// offending location, and it maintains a running count of open brackets so the
/// matching of <c>[</c> and <c>]</c> can be validated as it goes.
/// </para>
/// <para>
/// Any character that is not one of the eight instructions is silently ignored.
/// </para>
/// </remarks>
public class TextLexer: ILexer
{
    /// <summary>
    /// Scans <paramref name="input"/> into Brainfuck instructions.
    /// </summary>
    /// <param name="input">The source text to scan.</param>
    /// <returns>The instruction tokens in source order.</returns>
    /// <exception cref="UnmatchedOpeningBracketException">
    /// Thrown when a <c>]</c> appears with no matching <c>[</c>.
    /// </exception>
    /// <exception cref="UnmatchedClosingBracketException">
    /// Thrown at end of input when <c>[</c> brackets are still open.
    /// </exception>
    public IReadOnlyList<Token> ParseTokens(ReadOnlySpan<char> input)
    {
        var result = new List<Token>();
        var bracketWatcher = 0;
        var lines = 1;
        var characters = 0;
        foreach (var token in input)
        {
            // Column is 1-based, so increment before handling the character.
            characters += 1;
            switch (token)
            {
                case '>':
                    result.Add(Token.ShiftRight);
                    break;
                case '<':
                    result.Add(Token.ShiftLeft);
                    break;
                case '.':
                    result.Add(Token.Write);
                    break;
                case ',':
                    result.Add(Token.Read);
                    break;
                case '+':
                    result.Add(Token.Increment);
                    break;
                case '-':
                    result.Add(Token.Decrement);
                    break;
                case '[':
                    bracketWatcher += 1;
                    result.Add(Token.LeftBracket);
                    break;
                case ']':
                    bracketWatcher -= 1;
                    // A negative balance means this `]` closed nothing.
                    if (bracketWatcher < 0)
                    {
                        throw new UnmatchedOpeningBracketException(lines, characters);
                    }
                    result.Add(Token.RightBracket);
                    break;
                case '\n':
                    lines += 1;
                    characters = 0;
                    break;
            }
        }

        // Anything left open at the end of input has no matching `]`.
        if (bracketWatcher > 0)
        {
            throw new UnmatchedClosingBracketException(bracketWatcher);
        }
        return result;
    }

    /// <summary>
    /// Reports a <c>]</c> that has no matching <c>[</c>.
    /// </summary>
    /// <param name="line">The 1-based line of the offending <c>]</c>.</param>
    /// <param name="character">The 1-based column of the offending <c>]</c>.</param>
    /// <remarks>
    /// The type name says "opening" even though the cause is an extra closing
    /// bracket; see the bug report accompanying the documentation pass.
    /// </remarks>
    public class UnmatchedOpeningBracketException(int line, int character) :
        Exception($"Unmatched ']' at position ({line}:{character})");

    /// <summary>
    /// Reports one or more <c>[</c> brackets that are never closed.
    /// </summary>
    /// <param name="count">The number of still-open <c>[</c> brackets.</param>
    /// <remarks>
    /// The type name says "closing" even though the cause is missing closing
    /// brackets; see the bug report accompanying the documentation pass.
    /// </remarks>
    public class UnmatchedClosingBracketException(int count) : Exception($"Found {count} unmatched '['");
}
