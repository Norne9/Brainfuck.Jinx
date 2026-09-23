using Brainfuck.Jinx.Machine;

namespace Brainfuck.Jinx.Executor;

public interface IExecutor
{
    void Execute(IMachine machine, IReadOnlyList<OpCode> opcodes);
}