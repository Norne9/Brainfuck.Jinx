using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

/// <summary>
/// Folds a loop that merely walks the tape to the next zero cell
/// (<c>[&gt;]</c>, <c>[&lt;]</c>, <c>[&gt;&gt;]</c>, ...) into a single
/// <see cref="OpCodeType.PointerScan"/>.
/// </summary>
/// <remarks>
/// The loop <c>[&gt;&gt;]</c> has the same effect as "while the current cell is
/// non-zero, shift by two", which is exactly what the
/// <see cref="OpCodeType.PointerScan"/> op-code does. The body must be a single
/// <see cref="OpCodeType.Shift"/>; the simple parser has already merged any run
/// of <c>&gt;</c>/<c>&lt;</c> into one op, so this is the whole pointer-scan
/// idiom.
/// </remarks>
public sealed class PointerScanPattern : IPattern
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
        if (op.Type != OpCodeType.Loop || op.OpCodes is not { Count: 1 } body)
        {
            return false;
        }

        var scan = body[0];
        if (scan.Type != OpCodeType.Shift || scan.Value == 0)
        {
            return false;
        }

        consumed = 1;
        replacement = [new OpCode(OpCodeType.PointerScan, scan.Value)];
        return true;
    }
}
