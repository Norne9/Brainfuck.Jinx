using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

/// <summary>
/// Folds a loop that merely clears its counter cell (<c>[-]</c>) into a single
/// <see cref="OpCodeType.Set"/> of zero.
/// </summary>
/// <remarks>
/// This is the degenerate case of an arithmetic loop: the body changes the
/// counter by <c>-1</c> and leaves every other cell untouched. The resulting
/// <c>Set(0)</c> is usually fused with a following <see cref="OpCodeType.Add"/>
/// by <see cref="SetAddPattern"/>.
/// </remarks>
public sealed class ZeroLoopPattern : IPattern
{
    /// <inheritdoc />
    public bool TryMatch(
        IReadOnlyList<OpCode> opCodes,
        int index,
        in LoopAnalysis analysis,
        out int consumed,
        out OpCode[] replacement)
    {
        consumed = 0;
        replacement = [];

        var op = opCodes[index];
        if (op.OpCodes is null ||
            !analysis.IsArithmetic ||
            analysis.CounterDelta != -1 ||
            analysis.Destinations.Count != 0)
        {
            return false;
        }

        consumed = 1;
        replacement = [new OpCode(OpCodeType.Set, 0)];
        return true;
    }
}
