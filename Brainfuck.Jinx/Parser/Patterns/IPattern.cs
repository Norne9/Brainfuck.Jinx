using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

/// <summary>
/// A rewriting rule that recognises one op-code idiom and replaces it with an
/// equivalent, cheaper sequence of op-codes.
/// </summary>
/// <remarks>
/// <para>
/// Every implementation answers a single yes/no question: "does this op-code
/// match my idiom, and if so what replaces it?". The rule only ever consumes
/// the one op-code it matched, which keeps the contract small and lets the
/// parser apply a match with a single <c>RemoveAt</c>/<c>InsertRange</c> pair.
/// Returning an empty <paramref name="replacement"/> deletes the op-code.
/// </para>
/// <para>
/// Patterns are offered <b>every</b> op-code, not only loops. Some rules (for
/// example dropping a zero-valued <see cref="OpCodeType.Add"/>) only make sense
/// for flat op-codes, while others (for example
/// <see cref="PointerScanPattern"/>) only match a <see cref="OpCodeType.Loop"/>.
/// A rule simply returns <see langword="false"/> for op-codes it does not
/// handle.
/// </para>
/// <para>
/// The expensive part of matching a loop is inspecting its body, so the parser
/// computes a <see cref="LoopAnalysis"/> once for every loop and passes it to
/// every pattern. For non-loop op-codes the analysis is
/// <see langword="default"/>. Patterns that can decide from the raw op-code
/// simply ignore it.
/// </para>
/// </remarks>
public interface IPattern
{
    /// <summary>
    /// Tries to fold a single op-code into lower-level op-codes.
    /// </summary>
    /// <param name="op">The op-code to inspect.</param>
    /// <param name="analysis">
    /// A loop-body summary resolved once by the parser and shared across
    /// patterns. It is only meaningful when <paramref name="op"/> is a
    /// <see cref="OpCodeType.Loop"/>; otherwise it is <see langword="default"/>.
    /// </param>
    /// <param name="replacement">
    /// On success, the flat op-codes that replace <paramref name="op"/>; on
    /// failure, an empty array. An empty array on success deletes the op-code.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the op-code matched and
    /// <paramref name="replacement"/> is ready to use.
    /// </returns>
    bool TryMatch(OpCode op, in LoopAnalysis analysis, out OpCode[] replacement);
}
