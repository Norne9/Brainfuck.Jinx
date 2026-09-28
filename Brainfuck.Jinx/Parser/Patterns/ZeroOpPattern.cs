using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

/// <summary>
/// Deletes arithmetic op-codes that turn out to be no-ops: an
/// <see cref="OpCodeType.Add"/> or <see cref="OpCodeType.Shift"/> whose value is
/// zero.
/// </summary>
/// <remarks>
/// These fall out of <see cref="SimpleParser"/> when opposite runs cancel, for
/// example <c>+-</c> (an <c>Add(0)</c>) or <c>&gt;&lt;</c> (a <c>Shift(0)</c>).
/// They also routinely appear inside loop bodies, such as the trailing
/// <c>&lt;&lt;&gt;&gt;</c> of <c>[->&gt;+&lt;&lt;&gt;&gt;]</c>.
/// </remarks>
public sealed class ZeroOpPattern : IPattern
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
        if (op.Type is not (OpCodeType.Add or OpCodeType.Shift) || op.Value != 0)
        {
            return false;
        }

        consumed = 1;
        return true;
    }
}
