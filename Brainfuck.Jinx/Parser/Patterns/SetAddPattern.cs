using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

/// <summary>
/// Folds a <see cref="OpCodeType.Set"/> immediately followed by an
/// <see cref="OpCodeType.Add"/> into a single <see cref="OpCodeType.Set"/> whose
/// value is the sum.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="OpCodeType.Set"/> overwrites the current cell, so an addition
/// that follows it is a constant offset on top of the stored value:
/// <c>Set(x); Add(y)</c> is equivalent to <c>Set(x + y)</c>. The canonical case
/// is the <c>[-]</c> idiom: <see cref="ZeroLoopPattern"/> lowers it to
/// <c>Set(0)</c>, which is usually followed by an <see cref="OpCodeType.Add"/>,
/// so <c>[-]++</c> collapses to <c>Set(2)</c>.
/// </para>
/// <para>
/// This rule consumes two op-codes, as do <see cref="SetSetPattern"/> and
/// <see cref="AddSetPattern"/>. Chained additions are folded one at a time
/// because <see cref="OptimizingParser"/> repeats its traversal to a fixed point.
/// </para>
/// </remarks>
public sealed class SetAddPattern : IPattern
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

        if (index + 1 >= opCodes.Count)
        {
            return false;
        }

        var set = opCodes[index];
        var add = opCodes[index + 1];
        if (set.Type != OpCodeType.Set || add.Type != OpCodeType.Add)
        {
            return false;
        }

        consumed = 2;
        replacement = [new OpCode(OpCodeType.Set, set.Value + add.Value)];
        return true;
    }
}
