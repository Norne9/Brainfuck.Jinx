using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

/// <summary>
/// A rewriting rule that recognises one op-code idiom and replaces it with an
/// equivalent, cheaper sequence of op-codes.
/// </summary>
/// <remarks>
/// <para>
/// Every implementation answers a single yes/no question: "does the op-code run
/// starting here match my idiom, and if so what replaces it?". A rule may
/// consume the single op-code it starts on (the common case) or a short run of
/// adjacent op-codes, which is what lets the Set-focused rules fold an adjacent
/// <see cref="OpCodeType.Set"/> together with the <see cref="OpCodeType.Add"/>
/// that follows or precedes it. The parser applies a match with a single
/// <c>RemoveRange</c>/<c>InsertRange</c> pair. Returning an empty
/// <c>replacement</c> deletes the consumed run.
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
    /// Tries to fold the op-code run starting at <paramref name="index"/> into
    /// lower-level op-codes.
    /// </summary>
    /// <param name="opCodes">The program (or loop body) currently being optimised.</param>
    /// <param name="index">The index of the first op-code to inspect.</param>
    /// <param name="analysis">
    /// A loop-body summary resolved once by the parser and shared across
    /// patterns. It describes the op-code at <paramref name="index"/> and is only
    /// meaningful when that op-code is a <see cref="OpCodeType.Loop"/>; otherwise
    /// it is <see langword="default"/>.
    /// </param>
    /// <param name="consumed">
    /// On success, the number of op-codes starting at <paramref name="index"/>
    /// that <paramref name="replacement"/> replaces. Always at least one.
    /// </param>
    /// <param name="replacement">
    /// On success, the flat op-codes that replace the consumed run; on failure,
    /// an empty array. An empty array on success deletes the run.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the run matched and
    /// <paramref name="replacement"/> is ready to use.
    /// </returns>
    bool TryMatch(
        IReadOnlyList<OpCode> opCodes,
        int index,
        in LoopAnalysis analysis,
        out int consumed,
        out OpCode[] replacement);
}
