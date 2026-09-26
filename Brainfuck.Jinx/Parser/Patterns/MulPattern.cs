using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

/// <summary>
/// Folds an arithmetic loop that distributes its counter over two or more cells,
/// e.g. <c>[->+&gt;++&lt;&lt;]</c>, into a run of
/// <see cref="OpCodeType.Mul"/> op-codes followed by a final
/// <see cref="OpCodeType.MulAndClear"/>.
/// </summary>
/// <remarks>
/// Each destination gets its own <see cref="OpCodeType.Mul"/>. Only the last one
/// uses <see cref="OpCodeType.MulAndClear"/>, because the loop clears the
/// counter exactly once (at the end), not once per destination. The
/// destinations are already sorted by offset, so "last" is well defined.
/// </remarks>
public sealed class MulPattern : IPattern
{
    /// <inheritdoc />
    public bool TryMatch(OpCode op, in LoopAnalysis analysis, out OpCode[] replacement)
    {
        replacement = [];

        if (op.OpCodes is null ||
            !analysis.IsArithmetic ||
            analysis.CounterDelta != -1 ||
            analysis.Destinations.Count < 2)
        {
            return false;
        }

        var destinations = analysis.Destinations;
        var codes = new OpCode[destinations.Count];
        for (var i = 0; i < destinations.Count; i++)
        {
            var (buffer, value) = destinations[i];
            var type = i == destinations.Count - 1
                ? OpCodeType.MulAndClear
                : OpCodeType.Mul;
            codes[i] = new OpCode(type, value, null, 0, buffer);
        }

        replacement = codes;
        return true;
    }
}
