namespace Brainfuck.Jinx.Lexer;

public interface ILexer
{
    public IReadOnlyList<Token> ParseTokens(ReadOnlySpan<char> input);
}