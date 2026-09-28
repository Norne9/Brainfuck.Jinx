using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.Lexer;

namespace Brainfuck.Jinx.Parser;

/// <summary>
/// Converts a flat token stream into the executable op-code tree consumed by the
/// executors.
/// </summary>
/// <remarks>
/// <see cref="SimpleParser"/> performs only the trivial, always-valid peephole
/// rewrites. <see cref="OptimizingParser"/> extends it with structural idiom
/// recognition.
/// </remarks>
public interface IParser
{
    /// <summary>
    /// Parses <paramref name="tokens"/> into a top-level op-code list.
    /// </summary>
    /// <param name="tokens">The token stream produced by an <see cref="ILexer"/>.</param>
    /// <returns>The parsed program.</returns>
    List<OpCode> Parse(IReadOnlyList<Token> tokens);
}
