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

    private static string Run(string source, IEnumerable<byte>? input = null)
    {
        var tokens = new TextLexer().ParseTokens(source.AsSpan());
        var opCodes = new SimpleParser().Parse(tokens);
        using var io = new BufferedIo(input);
        var machine = new FixedMachine(io);

        new InterpreterExecutor().Execute(machine, opCodes);

        return Encoding.Latin1.GetString(io.Output.ToArray());
    }
}
