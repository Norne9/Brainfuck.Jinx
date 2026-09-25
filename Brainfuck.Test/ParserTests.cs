using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.Lexer;
using Brainfuck.Jinx.Parser;

namespace Brainfuck.Test;

public class ParserTests
{
    private readonly SimpleParser _parser = new();

    [Fact]
    public void Parse_MapsTokensToOpCodes()
    {
        var opCodes = _parser.Parse(
        [
            Token.Increment,
            Token.ShiftRight,
            Token.Write,
            Token.Read,
            Token.ShiftLeft,
            Token.Decrement
        ]);

        Assert.Collection(
            opCodes,
            code => AssertAdd(code, 1),
            code => AssertShift(code, 1),
            code => Assert.True(code.Type is OpCodeType.Write),
            code => Assert.True(code.Type is OpCodeType.Read),
            code => AssertShift(code, -1),
            code => AssertAdd(code, -1));
    }

    [Fact]
    public void Parse_CombinesConsecutiveAddsAndShifts()
    {
        var opCodes = _parser.Parse(
        [
            Token.Increment,
            Token.Increment,
            Token.Decrement,
            Token.ShiftRight,
            Token.ShiftRight,
            Token.ShiftLeft
        ]);

        Assert.Collection(
            opCodes,
            code => AssertAdd(code, 1),
            code => AssertShift(code, 1));
    }

    [Fact]
    public void Parse_BuildsNestedLoops()
    {
        var opCodes = _parser.Parse(
        [
            Token.Increment,
            Token.LeftBracket,
            Token.ShiftRight,
            Token.LeftBracket,
            Token.Decrement,
            Token.RightBracket,
            Token.RightBracket
        ]);

        Assert.Collection(
            opCodes,
            code => AssertAdd(code, 1),
            code =>
            {
                var outer = code;
                Assert.Collection(
                    outer.OpCodes!,
                    nested => AssertShift(nested, 1),
                    nested =>
                    {
                        Assert.Collection(
                            nested.OpCodes!,
                            add => AssertAdd(add, -1));
                    });
            });
    }

    [Fact]
    public void Parse_RemovesEmptyLoops()
    {
        var opCodes = _parser.Parse([Token.Increment, Token.LeftBracket, Token.RightBracket, Token.Write]);

        Assert.Collection(
            opCodes,
            code => AssertAdd(code, 1),
            code => Assert.True(code.Type is OpCodeType.Write));
    }

    private static void AssertAdd(OpCode code, int expected)
    {
        Assert.Equal(OpCodeType.Add, code.Type);
        Assert.Equal(expected, code.Value);
    }

    private static void AssertShift(OpCode code, int expected)
    {
        Assert.Equal(OpCodeType.Shift, code.Type);
        Assert.Equal(expected, code.Value);
    }
}
