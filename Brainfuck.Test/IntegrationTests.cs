using System.Text;
using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.Lexer;
using Brainfuck.Jinx.Machine;
using Brainfuck.Jinx.Parser;

namespace Brainfuck.Test;

public class IntegrationTests
{
    [Fact]
    public void TestsProgram_ReportsNewlineAndEofHandling()
    {
        const string source = ">,>+++++++++,>+++++++++++[<++++++<++++++<+>>>-]<<.>.<<-.>.>.<<.";

        var output = Run(source, [(byte)'\n', byte.MaxValue]);

        Assert.Equal("LA\nLA\n", output);
    }

    [Fact]
    public void TestsProgram_ReachesCellThirtyThousand()
    {
        const string source = """
                              ++++[>++++++<-]>[>+++++>+++++++<<-]>>++++<[[>[[>>+<<-]<]>>>-]>-[>+>+<<-]>]
                              +++++[>+++++++<<++>-]>.<<.
                              """;

        var output = Run(source);

        Assert.Equal("#\n", output);
    }

    [Fact]
    public void TestsProgram_HandlesObscureLoopCases()
    {
        const string source = """
                              []++++++++++[>>+>+>++++++[<<+<+++>>>-]<<<<-]
                              "A*$";?@![#>>+<<]>[>>]<<<<[>++<[-]]>.>.
                              """;

        var output = Run(source);

        Assert.Equal("H\n", output);
    }

    [Fact]
    public void NumwarpProgram_RendersWarpForTwo()
    {
        const string source = """
                              >>>>+>+++>+++>>>>>+++[
                                >,+>++++[>++++<-]>[<<[-[->]]>[<]>-]<<[
                                  >+>+>>+>+[<<<<]<+>>[+<]<[>]>+[[>>>]>>+[<<<<]>-]+<+>>>-[
                                    <<+[>]>>+<<<+<+<--------[
                                      <<-<<+[>]>+<<-<<-[
                                        <<<+<-[>>]<-<-<<<-<----[
                                          <<<->>>>+<-[
                                            <<<+[>]>+<<+<-<-[
                                              <<+<-<+[>>]<+<<<<+<-[
                                                <<-[>]>>-<<<-<-<-[
                                                  <<<+<-[>>]<+<<<+<+<-[
                                                    <<<<+[>]<-<<-[
                                                      <<+[>]>>-<<<<-<-[
                                                        >>>>>+<-<<<+<-[
                                                          >>+<<-[
                                                            <<-<-[>]>+<<-<-<-[
                                                              <<+<+[>]<+<+<-[
                                                                >>-<-<-[
                                                                  <<-[>]<+<++++[<-------->-]++<[
                                                                    <<+[>]>>-<-<<<<-[
                                                                      <<-<<->>>>-[
                                                                        <<<<+[>]>+<<<<-[
                                                                          <<+<<-[>>]<+<<<<<-[
                                                                            >>>>-<<<-<-
                              ]]]]]]]]]]]]]]]]]]]]]]>[>[[[<<<<]>+>>[>>>>>]<-]<]>>>+>>>>>>>+>]<
                              ]<[-]<<<<<<<++<+++<+++[
                                [>]>>>>>>++++++++[<<++++>++++++>-]<-<<[-[<+>>.<-]]<<<<[
                                  -[-[>+<-]>]>>>>>[.[>]]<<[<+>-]>>>[<<++[<+>--]>>-]
                                  <<[->+<[<++>-]]<<<[<+>-]<<<<
                                ]>>+>>>--[<+>---]<.>>[[-]<<]<
                              ]
                              [Enter a number using ()-./0123456789abcdef and space, and hit return.
                              Daniel B Cristofani (cristofdathevanetdotcom)
                              http://www.hevanet.com/cristofd/brainfuck/]
                              """;

        var output = Run(source, Encoding.ASCII.GetBytes("2\n"));

        Assert.Equal("/\\\n / \n \\/\n", output);
    }

    [Fact]
    public void OptimizingParser_PreservesNestedLoopSemantics()
    {
        // Regression: `[->[->+<]<]` is not a multiplication (the inner counter
        // is consumed on the first outer iteration), so the optimiser must not
        // assign it a closed form.
        const string source = "++++>+++<[->[->+<]<]>>.<.";

        var expected = Run(source);

        Assert.Equal(expected, RunOptimized(source, new InterpreterExecutor()));
        Assert.Equal(expected, RunOptimized(source, new JitExecutor()));
    }

    [Fact]
    public void OptimizingParser_CollapsesEmptiedLoopToHalt()
    {
        // +[><] : the body >< nets to nothing, so the loop is the `[]` idiom and
        // must become Halt. An empty Loop would hang the interpreter while the
        // JIT skipped it.
        const string source = "+[><]";

        var opCodes = new OptimizingParser().Parse(new TextLexer().ParseTokens(source.AsSpan()));

        Assert.Collection(
            opCodes,
            code => Assert.Equal(new OpCode(OpCodeType.Add, 1), code),
            code => Assert.Equal(OpCodeType.Halt, code.Type));

        // Both executors must terminate rather than spin.
        using var io = new BufferedIo();
        new InterpreterExecutor().Execute(new FixedMachine(io), opCodes);
        new JitExecutor().Execute(io, opCodes);
    }

    private static string Run(string source, IEnumerable<byte>? input = null)
    {
        var tokens = new TextLexer().ParseTokens(source.AsSpan());
        var opCodes = new SimpleParser().Parse(tokens);
        using var io = new BufferedIo(input);
        var machine = new FixedMachine(io);

        new InterpreterExecutor().Execute(machine, opCodes);

        return Encoding.Latin1.GetString(io.Output.ToArray());
    }

    [Fact]
    public void OptimizingParser_ProducesTheSameOutputAsSimpleParser()
    {
        const string source = """
                              []++++++++++[>>+>+>++++++[<<+<+++>>>-]<<<<-]
                              "A*$";?@![#>>+<<]>[>>]<<<<[>++<[-]]>.>.
                              """;

        var expected = Run(source);

        Assert.Equal(expected, RunOptimized(source, new InterpreterExecutor()));
        Assert.Equal(expected, RunOptimized(source, new JitExecutor()));
    }

    [Fact]
    public void OptimizingParser_ExecutesWalkingLoops()
    {
        // +>>+>>+>>+ fills every other cell, then [->>] walks across them and
        // zeroes each one as it passes.
        const string source = "+>>+>>+>>+[->>]<<<.";

        var expected = Run(source);

        Assert.Equal(expected, RunOptimized(source, new InterpreterExecutor()));
        Assert.Equal(expected, RunOptimized(source, new JitExecutor()));
    }

    private static string RunOptimized(string source, IExecutor executor)
    {
        var tokens = new TextLexer().ParseTokens(source.AsSpan());
        var opCodes = new OptimizingParser().Parse(tokens);
        using var io = new BufferedIo();
        var machine = new FixedMachine(io);

        executor.Execute(machine, opCodes);

        return Encoding.Latin1.GetString(io.Output.ToArray());
    }
}
