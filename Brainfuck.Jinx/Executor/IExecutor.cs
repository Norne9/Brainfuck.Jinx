using Brainfuck.Jinx.IO;
using Brainfuck.Jinx.Machine;

namespace Brainfuck.Jinx.Executor;

/// <summary>
/// Runs a flat, already-parsed list of <see cref="OpCode"/>s against either a
/// caller-supplied <see cref="IMachine"/> or a fresh machine backed by an
/// <see cref="IMachineIo"/>.
/// </summary>
/// <remarks>
/// Two implementations are provided: <see cref="InterpreterExecutor"/> is the
/// straightforward reference that dispatches each op-code through
/// <see cref="IMachine"/>, while <see cref="JitExecutor"/> compiles the program
/// to IL for speed. Both must produce identical observable behaviour; the
/// in-process tests in <c>JitInlineTests</c> guard that contract.
/// </remarks>
public interface IExecutor
{
    /// <summary>
    /// Executes <paramref name="opcodes"/> against an existing
    /// <paramref name="machine"/>, preserving the machine's current tape and
    /// pointer between calls.
    /// </summary>
    /// <param name="machine">The machine whose tape and pointer the program mutates.</param>
    /// <param name="opcodes">The parsed program to run.</param>
    void Execute(IMachine machine, IReadOnlyList<OpCode> opcodes);

    /// <summary>
    /// Executes <paramref name="opcodes"/> against a brand-new, zero-filled tape,
    /// reading and writing through <paramref name="io"/>.
    /// </summary>
    /// <param name="io">The input/output channel the program uses.</param>
    /// <param name="opcodes">The parsed program to run.</param>
    void Execute(IMachineIo io, IReadOnlyList<OpCode> opcodes);
}
