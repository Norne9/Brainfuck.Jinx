using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

/// <summary>
/// Folds a loop that moves (and clears) its counter into one destination cell,
/// e.g. <c>[->+&lt;]</c> or <c>[->-&lt;]</c>, into a single
/// <see cref="OpCodeType.MulAndClear"/>.
/// </summary>
public sealed class MulAndClearPattern : IPattern
{
    /// <inheritdoc />
    public bool TryMatch(OpCode loop, in LoopAnalysis analysis, out OpCode[] replacement)
    {
        replacement = [];

        if (loop.OpCodes is null ||
            !analysis.IsArithmetic ||
            analysis.CounterDelta != -1 ||
            analysis.Destinations.Count != 1)
        {
            return false;
        }

        var (buffer, value) = analysis.Destinations[0];
        replacement = [new OpCode(OpCodeType.MulAndClear, value, null, 0, buffer)];
        return true;
    }
}
