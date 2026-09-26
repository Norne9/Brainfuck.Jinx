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
    public bool TryMatch(OpCode op, in LoopAnalysis analysis, out OpCode[] replacement)
    {
        replacement = [];

        return op.Type is OpCodeType.Add or OpCodeType.Shift && op.Value == 0;
    }
}
