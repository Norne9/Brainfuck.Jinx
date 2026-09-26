using System.Text;
using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.Lexer;
using Brainfuck.Jinx.Machine;
using Brainfuck.Jinx.Parser;

namespace Brainfuck.Test;

/// <summary>
/// Guards the inlined <see cref="JitExecutor.Execute(Brainfuck.Jinx.IO.IMachineIo, IReadOnlyList{OpCode})"/>
/// path against the reference interpreter and the machine-based JIT path.
/// </summary>
public class JitInlineTests
{
    public static TheoryData<string, string, byte[]?> Programs
    {
        get
        {
            var data = new TheoryData<string, string, byte[]?>();
            foreach (var (label, source, input) in Cases)
            {
                data.Add(label, source, input);
            }

            return data;
        }
    }

    private static readonly (string Label, string Source, byte[]? Input)[] Cases =
    [
        ("obscure loops", """[]++++++++++[>>+>+>++++++[<<+<+++>>>-]<<<<-]"A*$";?@![#>>+<<]>[>>]<<<<[>++<[-]]>.>.""", null),
        ("wrap to cell 30000", """++++[>++++++<-]>[>+++++>+++++++<<-]>>++++<[[>[[>>+<<-]<]>>>-]>-[>+>+<<-]>]+++++[>+++++++<<++>-]>.<<.""", null),
        ("walking pointer scan", "+>>+>>+>>+[->>]<<<.", null),
        ("read and echo", ">,>+++++++++,>+++++++++++[<++++++<++++++<+>>>-]<<.>.<<-.>.>.<<.", [(byte)'\n', byte.MaxValue]),
        ("mul family", "+++++[->+>++<<]>>.<.<.", null),
        ("mul and mul", "++++[->[->+<]<]>>.<.", null)
    ];

    [Theory]
    [MemberData(nameof(Programs))]
    public void InlineExecutor_MatchesReferenceForSimpleParser(string label, string source, byte[]? input)
    {
        AssertEquivalent(label, source, input, new SimpleParser());
    }

    [Theory]
    [MemberData(nameof(Programs))]
    public void InlineExecutor_MatchesReferenceForOptimizingParser(string label, string source, byte[]? input)
    {
        AssertEquivalent(label, source, input, new OptimizingParser());
    }

    [Fact]
    public void InlineExecutor_AppliesMulOffsetAndHaltLikeTheInterpreter()
    {
        OpCode[] program =
        [
            new(OpCodeType.Add, 3),             // cell0 = 3
            new(OpCodeType.Shift, 1),
            new(OpCodeType.Add, 4),             // cell1 = 4
            new(OpCodeType.Shift, -1),          // back to cell0
            new(OpCodeType.Mul, 2, null, 2, 1), // cell1 += 2 * cell0, then shift by offset 2
            new(OpCodeType.Shift, -1),          // back to cell1
            new(OpCodeType.Write),              // cell1 = 4 + 2 * 3 = 10
            new(OpCodeType.Halt),               // cell1 != 0 => stop
            new(OpCodeType.Write)               // must not run
        ];

        using var inlineIo = new BufferedIo();
        new JitExecutor().Execute(inlineIo, program);

        using var machineIo = new BufferedIo();
        var machine = new FixedMachine(machineIo);
        new InterpreterExecutor().Execute(machine, program);

        Assert.Equal(machineIo.Output, inlineIo.Output);
        Assert.Equal([(byte)10], inlineIo.Output);
    }

    [Fact]
    public void InlineExecutor_DoesNotHaltWhenCellIsZero()
    {
        OpCode[] program =
        [
            new(OpCodeType.Halt),
            new(OpCodeType.Add, 5),
            new(OpCodeType.Write)
        ];

        using var inlineIo = new BufferedIo();
        new JitExecutor().Execute(inlineIo, program);

        Assert.Equal([(byte)5], inlineIo.Output);
    }

    private static void AssertEquivalent(string label, string source, byte[]? input, IParser parser)
    {
        var expected = RunMachine(source, input, parser, new InterpreterExecutor());
        var machineJit = RunMachine(source, input, parser, new JitExecutor());
        var inlineJit = RunInline(source, input, parser);

        Assert.True(
            expected.SequenceEqual(machineJit),
            $"{label}: machine JIT differs. expected=[{Convert.ToHexString(expected)}] actual=[{Convert.ToHexString(machineJit)}]");
        Assert.True(
            expected.SequenceEqual(inlineJit),
            $"{label}: inline JIT differs. expected=[{Convert.ToHexString(expected)}] actual=[{Convert.ToHexString(inlineJit)}]");
    }

    private static byte[] RunMachine(string source, byte[]? input, IParser parser, IExecutor executor)
    {
        var tokens = new TextLexer().ParseTokens(source.AsSpan());
        var opCodes = parser.Parse(tokens);
        using var io = new BufferedIo(input);
        var machine = new FixedMachine(io);
        executor.Execute(machine, opCodes);
        return io.Output.ToArray();
    }

    private static byte[] RunInline(string source, byte[]? input, IParser parser)
    {
        var tokens = new TextLexer().ParseTokens(source.AsSpan());
        var opCodes = parser.Parse(tokens);
        using var io = new BufferedIo(input);
        new JitExecutor().Execute(io, opCodes);
        return io.Output.ToArray();
    }
}
