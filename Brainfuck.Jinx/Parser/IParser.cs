using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.Lexer;

namespace Brainfuck.Jinx.Parser;

public interface IParser
{
    List<OpCode> Parse(IReadOnlyList<Token> tokens);
}