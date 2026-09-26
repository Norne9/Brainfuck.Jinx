using System.CommandLine;

using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.IO;
using Brainfuck.Jinx.Lexer;
using Brainfuck.Jinx.Parser;

namespace Brainfuck.Shell;

internal static class Program
{
    private static int Main(string[] args)
    {
        var sourceArgument = new Argument<string?>("source")
        {
            Description =
                "A Brainfuck source file path or a line of Brainfuck code. " +
                "If the value names an existing file the file is read, otherwise it is " +
                "treated as code. Use '-' to read the program from standard input.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var fileOption = new Option<FileInfo?>("--file", "-f")
        {
            Description = "Read the Brainfuck program from a file."
        };

        var codeOption = new Option<string?>("--code", "-c")
        {
            Description = "Run the given line of Brainfuck code."
        };

        var jitOption = new Option<bool>("--jit", "-j")
        {
            Description = "Execute the program with the JIT (IL) executor. This is the default."
        };

        var interpreterOption = new Option<bool>("--interpreter", "-i")
        {
            Description = "Execute the program with the tree-walking interpreter."
        };

        var opcodesOption = new Option<bool>("--opcodes", "-o")
        {
            Description = "Print the parsed op-codes instead of executing the program."
        };

        var rootCommand = new RootCommand(
            "Run Brainfuck programs from a file, an inline line of code, or standard input.")
        {
            sourceArgument,
            fileOption,
            codeOption,
            jitOption,
            interpreterOption,
            opcodesOption
        };

        rootCommand.SetAction(parseResult => Run(
            rootCommand,
            parseResult.GetValue(sourceArgument),
            parseResult.GetValue(fileOption),
            parseResult.GetValue(codeOption),
            parseResult.GetValue(jitOption),
            parseResult.GetValue(interpreterOption),
            parseResult.GetValue(opcodesOption)));

        return rootCommand.Parse(args).Invoke();
    }

    private static int Run(
        RootCommand rootCommand,
        string? source,
        FileInfo? file,
        string? code,
        bool jit,
        bool interpreter,
        bool opcodes)
    {
        if (jit && interpreter)
        {
            Console.Error.WriteLine("Options '--jit' and '--interpreter' cannot be used together.");
            return 1;
        }

        var provided = (source is null ? 0 : 1) + (file is null ? 0 : 1) + (code is null ? 0 : 1);
        if (provided == 0)
        {
            // Nothing to run: show the help text instead of blocking on standard input.
            return rootCommand.Parse(["--help"]).Invoke();
        }

        if (provided > 1)
        {
            Console.Error.WriteLine(
                "Specify only one of the 'source' argument, '--file', or '--code'.");
            return 1;
        }

        string program;
        try
        {
            program = ReadProgram(source, file, code);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Could not read the Brainfuck program: {ex.Message}");
            return 1;
        }

        IReadOnlyList<Token> tokens;
        try
        {
            tokens = new TextLexer().ParseTokens(program.AsSpan());
        }
        catch (TextLexer.UnmatchedOpeningBracketException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (TextLexer.UnmatchedClosingBracketException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        var opCodes = new OptimizingParser().Parse(tokens);

        if (opcodes)
        {
            PrintOpCodes(opCodes);
            return 0;
        }

        using var io = new BufferedIo();
        IExecutor executor = interpreter ? new InterpreterExecutor() : new JitExecutor();
        executor.Execute(io, opCodes);
        return 0;
    }

    /// <summary>
    /// Resolves the Brainfuck program from exactly one of the supported inputs.
    /// </summary>
    private static string ReadProgram(string? source, FileInfo? file, string? code)
    {
        if (code is not null)
        {
            return code;
        }

        if (file is not null)
        {
            return File.ReadAllText(file.FullName);
        }

        // Run guarantees exactly one input is set, so source is not null here.
        // '-' reads the program from standard input.
        if (source == "-")
        {
            return Console.In.ReadToEnd();
        }

        // Otherwise the value is either a path to a program or an inline program.
        return File.Exists(source) ? File.ReadAllText(source!) : source!;
    }

    private static void PrintOpCodes(IReadOnlyList<OpCode> opCodes)
    {
        foreach (var opCode in opCodes)
        {
            PrintOpCode(opCode, 0);
        }
    }

    private static void PrintOpCode(OpCode opCode, int depth)
    {
        var indent = new string(' ', depth * 2);

        if (opCode is { Type: OpCodeType.Loop, OpCodes: { Count: > 0 } body })
        {
            Console.WriteLine($"{indent}Loop");
            foreach (var inner in body)
            {
                PrintOpCode(inner, depth + 1);
            }
        }
        else
        {
            Console.WriteLine($"{indent}{opCode}");
        }
    }
}
