using Brainfuck.Jinx.Lexer;

namespace Brainfuck.Test;

public class LexerTests
{
    private readonly TextLexer _lexer = new();

    [Fact]
    public void ParseTokens_MapsEveryInstruction()
    {
        var tokens = _lexer.ParseTokens("<>+-[],.".AsSpan());

        Assert.Equal(
        [
            Token.ShiftLeft,
            Token.ShiftRight,
            Token.Increment,
            Token.Decrement,
            Token.LeftBracket,
            Token.RightBracket,
            Token.Read,
            Token.Write
        ], tokens);
    }

    [Fact]
    public void ParseTokens_IgnoresNonInstructionCharacters()
    {
        var tokens = _lexer.ParseTokens("some text + and more text >".AsSpan());

        Assert.Equal([Token.Increment, Token.ShiftRight], tokens);
    }

    [Fact]
    public void ParseTokens_AllowsNestedAndAdjacentLoops()
    {
        var tokens = _lexer.ParseTokens("[[]][]".AsSpan());

        Assert.Equal(
        [
            Token.LeftBracket,
            Token.LeftBracket,
            Token.RightBracket,
            Token.RightBracket,
            Token.LeftBracket,
            Token.RightBracket
        ], tokens);
    }

    [Fact]
    public void ParseTokens_ReportsUnexpectedClosingBracketPosition()
    {
        var exception = Assert.Throws<TextLexer.UnmatchedOpeningBracketException>(
            () => _lexer.ParseTokens("ignored\n  ]".AsSpan()));

        Assert.Equal("Unmatched ']' at position (2:3)", exception.Message);
    }

    [Fact]
    public void ParseTokens_ReportsNumberOfMissingClosingBrackets()
    {
        var exception = Assert.Throws<TextLexer.UnmatchedClosingBracketException>(
            () => _lexer.ParseTokens("[+[".AsSpan()));

        Assert.Equal("Found 2 unmatched '['", exception.Message);
    }
}
