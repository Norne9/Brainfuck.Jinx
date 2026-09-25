using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

/// <summary>
/// Folds a loop that merely clears its counter cell (<c>[-]</c>) into a single
/// <see cref="OpCodeType.SetZero"/>.
/// </summary>
/// <remarks>
/// This is the degenerate case of an arithmetic loop: the body changes the
/// counter by <c>-1</c> and leaves every other cell untouched.
/// </remarks>
public sealed class ZeroLoopPattern : IPattern
{
    /// <inheritdoc />
    public bool TryMatch(OpCode loop, in LoopAnalysis analysis, out OpCode[] replacement)
    {
        replacement = [];

        if (loop.OpCodes is null ||
            !analysis.IsArithmetic ||
            analysis.CounterDelta != -1 ||
            analysis.Destinations.Count != 0)
        {
            return false;
        }

        replacement = [new OpCode(OpCodeType.SetZero)];
        return true;
    }
}
