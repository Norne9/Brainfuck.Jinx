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
            code => Assert.True(code is OpCode.Write),
            code => Assert.True(code is OpCode.Read),
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
                var outer = GetLoop(code);
                Assert.Collection(
                    outer.opCodes,
                    nested => AssertShift(nested, 1),
                    nested =>
                    {
                        var inner = GetLoop(nested);
                        Assert.Collection(
                            inner.opCodes,
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
            code => Assert.True(code is OpCode.Write));
    }

    private static void AssertAdd(OpCode code, int expected)
    {
        var add = code switch
        {
            OpCode.Add value => value,
            _ => throw new InvalidOperationException($"Expected Add but found {code}")
        };
        Assert.Equal(expected, add.value);
    }

    private static void AssertShift(OpCode code, int expected)
    {
        var shift = code switch
        {
            OpCode.Shift value => value,
            _ => throw new InvalidOperationException($"Expected Shift but found {code}")
        };
        Assert.Equal(expected, shift.value);
    }

    private static OpCode.Loop GetLoop(OpCode code) => code switch
    {
        OpCode.Loop loop => loop,
        _ => throw new InvalidOperationException($"Expected Loop but found {code}")
    };
}
