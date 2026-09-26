using Brainfuck.Jinx.IO;
using Brainfuck.Jinx.Machine;

namespace Brainfuck.Jinx.Executor;

public interface IExecutor
{
    void Execute(IMachine machine, IReadOnlyList<OpCode> opcodes);
    void Execute(IMachineIo io, IReadOnlyList<OpCode> opcodes);
}