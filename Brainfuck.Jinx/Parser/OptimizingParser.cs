using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.Lexer;
using Brainfuck.Jinx.Parser.Patterns;

namespace Brainfuck.Jinx.Parser;

/// <summary>
/// Lowers a Brainfuck program to a flat op-code stream and then rewrites the
/// well-known idioms into cheaper, purpose-built op-codes.
/// </summary>
/// <remarks>
/// <para>
/// The base <see cref="SimpleParser"/> performs the trivial peephole rewrites
/// that never cross a loop boundary:
/// </para>
/// <list type="bullet">
///   <item><description>a run of <c>+</c>/<c>-</c> collapses into one <see cref="OpCodeType.Add"/>;</description></item>
///   <item><description>a run of <c>&gt;</c>/<c>&lt;</c> collapses into one <see cref="OpCodeType.Shift"/>;</description></item>
///   <item><description>a loop becomes an <see cref="OpCodeType.Loop"/> node whose <see cref="OpCode.OpCodes"/> are the op-codes of its body.</description></item>
/// </list>
/// <para>
/// This parser adds the structural rewrites. Each supported idiom is described
/// by an <see cref="IPattern"/> implementation. Patterns are offered
/// <b>every</b> op-code, not only loops, and are tried in order; the first match
/// wins:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="ZeroOpPattern"/>: <c>+0</c>/<c>&gt;0</c> (a cancelled run) &rarr; removed.</description></item>
///   <item><description><see cref="ZeroLoopPattern"/>: <c>[-]</c> &rarr; <see cref="OpCodeType.SetZero"/>.</description></item>
///   <item><description><see cref="MulAndClearPattern"/>: <c>[->+&lt;]</c> &rarr; <see cref="OpCodeType.MulAndClear"/>.</description></item>
///   <item><description><see cref="MulPattern"/>: <c>[->+&gt;++&lt;&lt;]</c> &rarr; a run of <see cref="OpCodeType.Mul"/> with a final <see cref="OpCodeType.MulAndClear"/>.</description></item>
///   <item><description><see cref="MulAndMulPattern"/>: <c>[->[->+&lt;]&lt;]</c> &rarr; <see cref="OpCodeType.MulAndMul"/> followed by the inner loop and a <see cref="OpCodeType.SetZero"/>.</description></item>
///   <item><description><see cref="PointerScanPattern"/>: <c>[&gt;]</c> &rarr; <see cref="OpCodeType.PointerScan"/>.</description></item>
/// </list>
/// <para>
/// Recognising a loop pattern is dominated by inspecting the loop body, so the
/// parser summarises each body exactly once into a <see cref="LoopAnalysis"/>
/// and hands that same value to every pattern. Patterns then decide from the
/// shared summary instead of re-scanning the body. Non-loop op-codes are handed
/// a <see langword="default"/> analysis.
/// </para>
/// <para>
/// Optimisation proceeds <b>bottom-up</b>: a loop's body is rewritten before
/// the loop itself is offered to the patterns. This ordering is required by
/// <see cref="MulAndMulPattern"/>, which only recognises an inner loop once that
/// loop has already been lowered to a <see cref="OpCodeType.Mul"/> or
/// <see cref="OpCodeType.MulAndClear"/>.
/// </para>
/// <para>
/// A replacement is always flat (patterns never introduce a new loop), but a
/// flat replacement can itself be recognised by a later rule -- for example a
/// deleted run can leave a loop body empty. <see cref="Parse"/> therefore
/// repeats the traversal until a pass makes no change.
/// </para>
/// </remarks>
public class OptimizingParser : SimpleParser
{
    /// <summary>
    /// The pattern handlers, tried in order for every op-code. The first match
    /// wins. The list is created once and reused for every parse.
    /// </summary>
    private static readonly IPattern[] Patterns =
    [
        // +0 / >0 (cancelled run) => (removed)
        new ZeroOpPattern(),

        // [-] => SetZero
        new ZeroLoopPattern(),

        // [->+<] => MulAndClear
        new MulAndClearPattern(),

        // [->+>++<<] => Mul(...) + MulAndClear(...)
        new MulPattern(),

        // [->[->+<]<] => MulAndMul(...) + MulAndClear(...) + SetZero
        new MulAndMulPattern(),

        // [>] => PointerScan
        new PointerScanPattern()
    ];

    /// <summary>
    /// Parses <paramref name="tokens"/> and optimises the resulting op-code tree
    /// in place.
    /// </summary>
    /// <param name="tokens">The token stream produced by a lexer.</param>
    /// <returns>The optimised, flat top-level op-code list.</returns>
    /// <remarks>
    /// <see cref="SimpleParser.Parse"/> builds a brand-new tree and every loop
    /// body is its own <see cref="List{T}"/>, so the result can be mutated
    /// directly without copying it first. The traversal is repeated until it
    /// reaches a fixed point.
    /// </remarks>
    public override List<OpCode> Parse(IReadOnlyList<Token> tokens)
    {
        var opCodes = base.Parse(tokens);
        while (Optimize(opCodes));
        return opCodes;
    }

    /// <summary>
    /// Rewrites <paramref name="opCodes"/> and all of its nested loop bodies in
    /// place, replacing every recognised idiom with its lower-level form.
    /// </summary>
    /// <param name="opCodes">The program (or loop body) to optimise.</param>
    /// <returns>
    /// <see langword="true"/> if at least one op-code was rewritten; otherwise
    /// <see langword="false"/>.
    /// </returns>
    private static bool Optimize(List<OpCode> opCodes)
    {
        var madeChanges = false;

        // Visit op-codes left-to-right. Replacing an element inserts flat
        // op-codes only, so any inserted element that a later rule can match is
        // picked up by the next pass (Parse loops until no change).
        for (var i = 0; i < opCodes.Count; i++)
        {
            // Bottom-up: descend into a loop body first. By the time the loop
            // itself is offered to the patterns, any nested idiom it contains
            // has already been rewritten to the flat form that patterns such as
            // MulAndMulPattern expect. Non-loop op-codes get a default
            // analysis.
            var analysis = default(LoopAnalysis);
            if (opCodes[i] is { Type: OpCodeType.Loop, OpCodes: { Count: > 0 } body })
            {
                madeChanges |= Optimize(body);
                analysis = LoopAnalysis.Analyze(body);
            }

            // Try the patterns in order and stop at the first match. Once a
            // pattern succeeds the slot no longer represents the construct the
            // remaining patterns look for, so trying them would be pointless.
            foreach (var pattern in Patterns)
            {
                if (!pattern.TryMatch(opCodes[i], in analysis, out var replacement))
                {
                    continue;
                }

                // Every pattern replaces exactly the op-code it matched. An
                // empty replacement deletes it.
                opCodes.RemoveAt(i);
                if (replacement.Length > 0)
                {
                    opCodes.InsertRange(i, replacement);
                }

                madeChanges = true;
                break;
            }
        }

        return madeChanges;
    }
}
