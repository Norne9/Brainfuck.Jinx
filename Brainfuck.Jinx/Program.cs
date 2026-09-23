using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.IO;
using Brainfuck.Jinx.Lexer;
using Brainfuck.Jinx.Machine;
using Brainfuck.Jinx.Parser;

namespace Brainfuck.Jinx;

class Program
{
    private const string TestProgram = """
                                       ++++[>+++++<-]>[<+++++>-]+<+[
                                           >[>+>+<<-]++>>[<<+>>-]>>>[-]++>[-]+
                                           >>>+[[-]++++++>>>]<<<[[<++++++++<++>>-]+<.<[>----<-]<]
                                           <<[>>>>>[>>>[-]+++++++++<[>-<-]+++++++++>[-[<->-]+[<<<]]<[>+<-]>]<<-]<<-
                                       ]
                                       [Outputs square numbers from 0 to 10000.
                                       Daniel B Cristofani (cristofdathevanetdotcom)
                                       http://www.hevanet.com/cristofd/brainfuck/]
                                       """;
    static void Main(string[] args)
    {
        var lexer = new TextLexer();
        var tokens = lexer.ParseTokens(TestProgram.AsSpan());
        var parser = new SimpleParser();
        var codes = parser.Parse(tokens);
        using var io = new SimpleIo();
        var machine = new FixedMachine(io);
        var executor = new InterpreterExecutor();
        executor.Execute(machine, codes);
    }
}