using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.Lexer;

namespace Brainfuck.Jinx.Parser;

public interface IParser
{
    IReadOnlyList<OpCode> Parse(IReadOnlyList<Token> tokens);
}