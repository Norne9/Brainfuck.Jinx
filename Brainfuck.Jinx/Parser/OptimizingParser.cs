using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.Lexer;
using Brainfuck.Jinx.Parser.Patterns;

namespace Brainfuck.Jinx.Parser;

/// <summary>
/// Lowers a Brainfuck program to a flat op-code stream and then rewrites the
/// well-known loop idioms into cheaper, purpose-built op-codes.
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
/// by an <see cref="IPattern"/> implementation, listed from most specific to
/// most general:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="ZeroLoopPattern"/>: <c>[-]</c> &rarr; <see cref="OpCodeType.SetZero"/>.</description></item>
///   <item><description><see cref="MulAndClearPattern"/>: <c>[->+&lt;]</c> &rarr; <see cref="OpCodeType.MulAndClear"/>.</description></item>
///   <item><description><see cref="MulPattern"/>: <c>[->+&gt;++&lt;&lt;]</c> &rarr; a run of <see cref="OpCodeType.Mul"/> with a final <see cref="OpCodeType.MulAndClear"/>.</description></item>
///   <item><description><see cref="MulAndMulPattern"/>: <c>[->[->+&lt;]&lt;]</c> &rarr; <see cref="OpCodeType.MulAndMul"/> followed by the inner loop and a <see cref="OpCodeType.SetZero"/>.</description></item>
/// </list>
/// <para>
/// Recognising a pattern is dominated by inspecting the loop body, so the parser
/// summarises each body exactly once into a <see cref="LoopAnalysis"/> and hands
/// that same value to every pattern. Patterns then decide from the shared
/// summary instead of re-scanning the body.
/// </para>
/// <para>
/// Optimisation proceeds <b>bottom-up</b>: a loop's body is rewritten before
/// the loop itself is offered to the patterns. This ordering is required by
/// <see cref="MulAndMulPattern"/>, which only recognises an inner loop once that
/// loop has already been lowered to a <see cref="OpCodeType.Mul"/> or
/// <see cref="OpCodeType.MulAndClear"/>.
/// </para>
/// <para>
/// A single bottom-up traversal reaches a fixed point. Patterns only ever
/// replace an <see cref="OpCodeType.Loop"/> node with flat op-codes and never
/// introduce a new loop, so rewriting one node can create a match only in an
/// ancestor (already visited afterwards), never in a sibling or a descendant
/// (already visited before). No repeated whole-tree pass is therefore necessary.
/// </para>
/// </remarks>
public class OptimizingParser : SimpleParser
{
    /// <summary>
    /// The pattern handlers, tried in order for every loop. The first match
    /// wins. <see cref="ZeroLoopPattern"/>, <see cref="MulAndClearPattern"/> and
    /// <see cref="MulPattern"/> are mutually exclusive (they split on the number
    /// of destinations), while <see cref="MulAndMulPattern"/> handles the one
    /// idiom that is not a flat arithmetic loop. The list is created once and
    /// reused for every parse.
    /// </summary>
    private static readonly IPattern[] Patterns =
    [
        // [-] => SetZero
        new ZeroLoopPattern(),

        // [->+<] => MulAndClear
        new MulAndClearPattern(),

        // [->+>++<<] => Mul(...) + MulAndClear(...)
        new MulPattern(),

        // [->[->+<]<] => MulAndMul(...) + MulAndClear(...) + SetZero
        new MulAndMulPattern()
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
    /// directly without copying it first.
    /// </remarks>
    public override List<OpCode> Parse(IReadOnlyList<Token> tokens)
    {
        var opCodes = base.Parse(tokens);
        while (Optimize(opCodes));
        return opCodes;
    }

    /// <summary>
    /// Rewrites <paramref name="opCodes"/> and all of its nested loop bodies in
    /// place, replacing every recognised loop idiom with its lower-level form.
    /// </summary>
    /// <param name="opCodes">The program (or loop body) to optimise.</param>
    /// <returns>
    /// <see langword="true"/> if at least one loop was replaced by a pattern;
    /// otherwise <see langword="false"/>.
    /// </returns>
    private static bool Optimize(List<OpCode> opCodes)
    {
        var madeChanges = false;

        // Visit siblings left-to-right. Replacing an element never creates a
        // new Loop node, so a single forward pass is enough: any inserted
        // op-codes are flat and cannot match a pattern themselves.
        for (var i = 0; i < opCodes.Count; i++)
        {
            // Patterns only ever apply to loops, so skip everything else up
            // front instead of making each pattern re-check the op-code.
            if (opCodes[i] is not { Type: OpCodeType.Loop, OpCodes: { Count: > 0 } body })
            {
                continue;
            }

            // Bottom-up: descend into the body first. By the time this loop is
            // offered to the patterns, any nested loop it contains has already
            // been rewritten to the flat form that MulAndMulPattern expects.
            madeChanges |= Optimize(body);

            // Summarise the (now final) body once and share it across all
            // patterns. Without this, the two multiplication patterns would
            // each analyse the same body.
            var analysis = LoopAnalysis.Analyze(body);

            // Try the patterns in order and stop at the first match. Once a
            // pattern succeeds the slot no longer represents the construct the
            // remaining patterns look for, so trying them would be pointless.
            foreach (var pattern in Patterns)
            {
                if (!pattern.TryMatch(opCodes[i], in analysis, out var replacement))
                {
                    continue;
                }

                // Every pattern replaces exactly the loop it matched.
                opCodes.RemoveAt(i);
                opCodes.InsertRange(i, replacement);

                madeChanges = true;
                break;
            }
        }

        return madeChanges;
    }
}
