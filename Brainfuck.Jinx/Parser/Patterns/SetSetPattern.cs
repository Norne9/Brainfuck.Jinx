using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

/// <summary>
/// Folds two consecutive <see cref="OpCodeType.Set"/> op-codes into the second
/// one: <c>Set(x); Set(y)</c> is just <c>Set(y)</c>.
/// </summary>
/// <remarks>
/// The first store is invisible once the second overwrites the same cell, so its
/// value is discarded. Because the op-codes are adjacent there is no
/// <see cref="OpCodeType.Shift"/> between them and both target the same cell.
/// </remarks>
public sealed class SetSetPattern : IPattern
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

        var first = opCodes[index];
        var second = opCodes[index + 1];
        if (first.Type != OpCodeType.Set || second.Type != OpCodeType.Set)
        {
            return false;
        }

        consumed = 2;
        replacement = [second];
        return true;
    }
}
