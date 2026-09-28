using System.Diagnostics;
using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.Lexer;
using Brainfuck.Jinx.Machine;
using Brainfuck.Jinx.Parser;

namespace Brainfuck.Test;

public class SpeedRegressionTests
{
    [Fact]
    [Trait("Category", "SpeedRegression")]
    public void LexerAndParser_ProcessOneMillionInstructionsWithinBudget()
    {
        var source = string.Concat(Enumerable.Repeat("++++>>>>----<<<<", 62_500));
        var lexer = new TextLexer();
        var parser = new SimpleParser();

        _ = parser.Parse(lexer.ParseTokens("+-<>".AsSpan()));
        var stopwatch = Stopwatch.StartNew();
        var tokens = lexer.ParseTokens(source.AsSpan());
        var opCodes = parser.Parse(tokens);
        stopwatch.Stop();

        Assert.Equal(1_000_000, tokens.Count);
        Assert.Equal(250_000, opCodes.Count);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"Lexing and parsing took {stopwatch.Elapsed.TotalSeconds:F2}s; budget is 5.00s.");
    }

    [Fact]
    [Trait("Category", "SpeedRegression")]
    public void Executor_PerformsTenMillionLoopOperationsWithinBudget()
    {
        using var io = new BufferedIo();
        var machine = new FixedMachine(io);
        var executor = new InterpreterExecutor();
        OpCode[] program =
        [
            new(OpCodeType.Add, 100),
            new OpCode(OpCodeType.Loop, 0, [new OpCode(OpCodeType.Add, -1)])
        ];

        executor.Execute(machine, program);
        var stopwatch = Stopwatch.StartNew();
        for (var iteration = 0; iteration < 100_000; iteration++)
        {
            executor.Execute(machine, program);
        }

        stopwatch.Stop();

        Assert.True(machine.IsZero());
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"Executing 10,000,000 loop operations took {stopwatch.Elapsed.TotalSeconds:F2}s; budget is 5.00s.");
    }
}
