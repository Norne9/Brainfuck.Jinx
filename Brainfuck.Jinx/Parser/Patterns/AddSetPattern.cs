using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

/// <summary>
/// Deletes an <see cref="OpCodeType.Add"/> that is immediately overwritten by a
/// <see cref="OpCodeType.Set"/>: <c>Add(y); Set(x)</c> is just <c>Set(x)</c>.
/// </summary>
/// <remarks>
/// A <see cref="OpCodeType.Set"/> replaces the current cell unconditionally, so
/// the addition that precedes it has no observable effect. The canonical case is
/// <c>+[-]</c>: <see cref="ZeroLoopPattern"/> lowers the loop to <c>Set(0)</c>,
/// which makes the leading <c>Add(1)</c> dead.
/// </remarks>
public sealed class AddSetPattern : IPattern
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

        var add = opCodes[index];
        var set = opCodes[index + 1];
        if (add.Type != OpCodeType.Add || set.Type != OpCodeType.Set)
        {
            return false;
        }

        // Consume the dead Add; the Set survives untouched.
        consumed = 2;
        replacement = [set];
        return true;
    }
}
