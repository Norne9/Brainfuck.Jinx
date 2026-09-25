using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

public interface IPattern
{
    (int count, List<OpCode>? newCodes) Apply(List<OpCode> opcodes, int offset);
}