using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

/// <summary>
/// A rewriting rule that recognises one loop idiom and replaces it with an
/// equivalent, flat sequence of op-codes.
/// </summary>
/// <remarks>
/// <para>
/// Every implementation answers a single yes/no question: "does this loop match
/// my idiom, and if so what replaces it?". The rule only ever consumes the one
/// loop it matched, which keeps the contract small and lets the parser apply a
/// match with a single <c>RemoveAt</c>/<c>InsertRange</c> pair.
/// </para>
/// <para>
/// The expensive part of matching is inspecting the loop body, so the parser
/// computes a <see cref="LoopAnalysis"/> once and passes it to every pattern.
/// Patterns that can decide from the raw body shape simply ignore the analysis.
/// </para>
/// </remarks>
public interface IPattern
{
    /// <summary>
    /// Tries to fold a single loop into lower-level op-codes.
    /// </summary>
    /// <param name="loop">
    /// The loop to inspect. The parser only calls this for
    /// <see cref="OpCodeType.Loop"/> op-codes whose body is non-empty.
    /// </param>
    /// <param name="analysis">
    /// A body summary resolved once by the parser and shared across patterns.
    /// </param>
    /// <param name="replacement">
    /// On success, the flat op-codes that replace <paramref name="loop"/>; on
    /// failure, an empty array.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the loop matched and
    /// <paramref name="replacement"/> is ready to use.
    /// </returns>
    bool TryMatch(OpCode loop, in LoopAnalysis analysis, out OpCode[] replacement);
}
