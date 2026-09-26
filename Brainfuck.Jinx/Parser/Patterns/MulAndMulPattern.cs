using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

/// <summary>
/// Folds a loop whose body already contains a flattened inner multiplication,
/// e.g. <c>[->[->+&lt;]&lt;]</c>, into a <see cref="OpCodeType.MulAndMul"/>
/// followed by the inner multiplication and a <see cref="OpCodeType.Set"/> of
/// zero.
/// </summary>
/// <remarks>
/// This idiom cannot be described by <see cref="LoopAnalysis"/> because the body
/// contains an already-flattened op-code rather than plain <c>Add</c>/<c>Shift</c>.
/// It is therefore the one pattern that scans the raw body itself. It only
/// matches after the nested loop has been folded, which is guaranteed by the
/// parser's bottom-up traversal order.
/// </remarks>
public sealed class MulAndMulPattern : IPattern
{
    /// <inheritdoc />
    public bool TryMatch(
        IReadOnlyList<OpCode> opCodes,
        int index,
        in LoopAnalysis analysis,
        out int consumed,
        out OpCode[] replacement)
    {
        // The shared arithmetic analysis intentionally does not apply here; this
        // pattern inspects the raw body below.
        consumed = 0;
        replacement = [];

        var op = opCodes[index];
        if (op.OpCodes is not { } body)
        {
            return false;
        }

        var pointer = 0;
        var counterDelta = 0;
        OpCode? flattened = null;
        var flattenedAt = 0;

        foreach (var code in body)
        {
            switch (code.Type)
            {
                case OpCodeType.Shift:
                    pointer += code.Value;
                    break;

                // Only increments on the counter cell may remain un-flattened;
                // any addition elsewhere means this is not the idiom.
                case OpCodeType.Add when pointer == 0:
                    counterDelta += code.Value;
                    break;

                // The single flattened inner multiplication, anchored on the
                // counter cell (Offset == 0) so it reads the counter's value.
                case OpCodeType.Mul or OpCodeType.MulAndClear when flattened is null && code.Offset == 0:
                    flattened = code;
                    flattenedAt = pointer;
                    break;

                default:
                    return false;
            }
        }

        if (flattened is not { } inner || pointer != 0 || counterDelta != -1)
        {
            return false;
        }

        consumed = 1;
        replacement =
        [
            // Increase the target by the counter, then multiply by it.
            new OpCode(OpCodeType.MulAndMul, 1, null, flattenedAt, flattenedAt),

            // Replay the inner multiplication at its (now shifted) position.
            inner with { Offset = -flattenedAt },

            // The outer counter is consumed.
            new OpCode(OpCodeType.Set, 0)
        ];

        return true;
    }
}
