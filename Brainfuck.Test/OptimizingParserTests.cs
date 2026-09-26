using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.Lexer;
using Brainfuck.Jinx.Parser;

namespace Brainfuck.Test;

public class OptimizingParserTests
{
    [Fact]
    public void Parse_FlattensSingleDestinationLoop()
    {
        AssertFlattened("[->+<]", new OpCode(OpCodeType.MulAndClear, 1, null, 0, 1));
    }

    [Fact]
    public void Parse_FlattensNegativeBuffer()
    {
        AssertFlattened("[-<+>]", new OpCode(OpCodeType.MulAndClear, 1, null, 0, -1));
    }

    [Fact]
    public void Parse_FlattensNegativeValue()
    {
        AssertFlattened("[->>-<<]", new OpCode(OpCodeType.MulAndClear, -1, null, 0, 2));
    }

    [Fact]
    public void Parse_FlattensMultipleDestinations()
    {
        var tokens = new TextLexer().ParseTokens("+[->+>++<<]".AsSpan());
        var opCodes = new OptimizingParser().Parse(tokens);

        Assert.Collection(
            opCodes,
            code => Assert.Equal(new OpCode(OpCodeType.Add, 1), code),
            code => Assert.Equal(new OpCode(OpCodeType.Mul, 1, null, 0, 1), code),
            code => Assert.Equal(new OpCode(OpCodeType.MulAndClear, 2, null, 0, 2), code));
    }

    [Fact]
    public void Parse_FlattensNestedLoop()
    {
        var tokens = new TextLexer().ParseTokens("+[->[->+<]<]".AsSpan());
        var opCodes = new OptimizingParser().Parse(tokens);

        Assert.Collection(
            opCodes,
            code => Assert.Equal(new OpCode(OpCodeType.Add, 1), code),
            code => Assert.Equal(new OpCode(OpCodeType.MulAndMul, 1, null, 1, 1), code),
            code => Assert.Equal(new OpCode(OpCodeType.MulAndClear, 1, null, -1, 1), code),
            code => Assert.Equal(new OpCode(OpCodeType.SetZero), code));
    }

    [Fact]
    public void Parse_StillFlattensZeroLoop()
    {
        AssertFlattened("[-]", new OpCode(OpCodeType.SetZero));
    }

    [Fact]
    public void Parse_FlattensZeroLoopWithNetZeroPointerMoves()
    {
        // [><-] moves right then back before clearing the counter, so it has the
        // same net effect as [-].
        AssertFlattened("[><-]", new OpCode(OpCodeType.SetZero));
    }

    [Fact]
    public void Parse_KeepsUnrecognisedLoop()
    {
        var tokens = new TextLexer().ParseTokens("+[--]".AsSpan());
        var opCodes = new OptimizingParser().Parse(tokens);

        Assert.Collection(
            opCodes,
            code => Assert.Equal(new OpCode(OpCodeType.Add, 1), code),
            code =>
            {
                Assert.Equal(OpCodeType.Loop, code.Type);
                Assert.Equal(new OpCode(OpCodeType.Add, -2), code.OpCodes![0]);
            });
    }

    [Fact]
    public void Parse_FoldsPointerScanLoop()
    {
        AssertFlattened("[>]", new OpCode(OpCodeType.PointerScan, 1));
    }

    [Fact]
    public void Parse_FoldsPointerScanWithStride()
    {
        AssertFlattened("[<<<]", new OpCode(OpCodeType.PointerScan, -3));
    }

    [Fact]
    public void Parse_KeepsWalkingLoopAsGenericLoop()
    {
        // A walking loop shifts the pointer every iteration, so it cannot be
        // flattened into fixed op-codes. The generic Loop already reproduces
        // that drift, so it is left alone; the cancelled <<>> is still dropped.
        var tokens = new TextLexer().ParseTokens("+[->>+<<>>]".AsSpan());
        var opCodes = new OptimizingParser().Parse(tokens);

        Assert.Equal(2, opCodes.Count);
        Assert.Equal(new OpCode(OpCodeType.Add, 1), opCodes[0]);
        Assert.Equal(OpCodeType.Loop, opCodes[1].Type);
        Assert.Equal(
        [
            new OpCode(OpCodeType.Add, -1),
            new OpCode(OpCodeType.Shift, 2),
            new OpCode(OpCodeType.Add, 1)
        ], opCodes[1].OpCodes);
    }

    [Fact]
    public void Parse_DropsCancelledAddRun()
    {
        var tokens = new TextLexer().ParseTokens("+-".AsSpan());
        var opCodes = new OptimizingParser().Parse(tokens);

        Assert.Empty(opCodes);
    }

    [Fact]
    public void Parse_DropsCancelledShiftRun()
    {
        var tokens = new TextLexer().ParseTokens("><".AsSpan());
        var opCodes = new OptimizingParser().Parse(tokens);

        Assert.Empty(opCodes);
    }

    // The simple parser drops a leading loop, so every sample is prefixed
    // with a no-op Add that the assertions skip.
    private static void AssertFlattened(string loop, OpCode expected)
    {
        var tokens = new TextLexer().ParseTokens(("+" + loop).AsSpan());
        var opCodes = new OptimizingParser().Parse(tokens);

        Assert.Equal(2, opCodes.Count);
        Assert.Equal(new OpCode(OpCodeType.Add, 1), opCodes[0]);
        Assert.Equal(expected, opCodes[1]);
    }
}
