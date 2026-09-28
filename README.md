# Brainfuck.Jinx

A small, well-documented **Brainfuck toolchain for .NET**: a lexer, an
optimizing parser, two interchangeable executors (a reference interpreter and an
IL-emitting JIT), and a command-line `bf` shell.

Brainfuck.Jinx is built to be read as much as it is to be run. Every public
type carries XML documentation, the semantics are specified rather than
implied, and the JIT is tested to be observably identical to the interpreter.

---

## Table of contents

- [What is this?](#what-is-this)
- [Features](#features)
- [Requirements](#requirements)
- [Building](#building)
- [Running tests](#running-tests)
- [Using the command-line shell](#using-the-command-line-shell)
  - [Input sources](#input-sources)
  - [Executors](#executors)
  - [Inspecting optimized op-codes](#inspecting-optimized-op-codes)
  - [Examples](#examples)
- [Using the library](#using-the-library)
- [How it works](#how-it-works)
  - [Pipeline](#pipeline)
  - [Lexer](#lexer)
  - [Parsers](#parsers)
  - [The optimizer](#the-optimizer)
  - [Op-codes](#op-codes)
  - [Machine model](#machine-model)
  - [Executors](#executors)
  - [I/O](#io)
- [Repository layout](#repository-layout)
- [Sample programs](#sample-programs)
- [License](#license)

---

## What is this?

Brainfuck is an eight-instruction esoteric programming language. This project
implements a complete, standards-compliant Brainfuck runtime:

- **`Brainfuck.Jinx`** — the core class library. Parse Brainfuck source into an
  optimized op-code tree, then execute it with either the interpreter or the
  JIT. The library is engine-agnostic: the tape and I/O channel are interfaces
  you can supply.
- **`Brainfuck.Shell`** — a `System.CommandLine`-based CLI that runs `.b` files,
  inline snippets, or standard input, and can disassemble the optimized program.
- **`Brainfuck.Test`** — an xUnit v3 suite covering the lexer, parsers,
  machine, both executors, cross-executor equivalence, and performance budgets.

The name *Jinx* refers to the JIT: programs are compiled to IL at run time with
`System.Reflection.Emit.DynamicMethod`, giving a large speed-up over tree-walking
without leaving managed code.

## Features

- **Two parsers.** `SimpleParser` performs always-valid peephole rewrites;
  `OptimizingParser` adds structural idiom recognition (`[-]`, `[->+<]`,
  `[>]`, …) driven by pluggable `IPattern` rules.
- **Two executors.** `InterpreterExecutor` is the clear reference implementation;
  `JitExecutor` emits IL and has a fast path that inlines the tape and pointer
  as locals.
- **Guaranteed equivalence.** The test suite runs the same programs through the
  interpreter, the machine-based JIT, and the inlined JIT, and asserts identical
  output.
- **A real fixed-tape machine.** 30,000 byte cells, byte values wrap modulo 256,
  and the pointer wraps around the tape ring.
- **Clean I/O abstraction.** `IMachineIo` decouples programs from the console,
  so you can capture output in a string, feed scripted input, or drive a custom
  terminal.
- **Correct-on-zero.** Programs that start on a zero cell, empty loops (`[]`),
  and unbalanced-pointer idioms (`[>>]`) are handled without hanging or crashing.
- **No third-party runtime dependencies** in the core library.

## Requirements

- **.NET SDK 10.0 or later.** The projects target `net10.0`. Newer SDKs
  (including .NET 11 previews) build and run the solution unchanged because the
  solution does not pin an SDK version.
- Any OS supported by .NET: Windows, Linux, and macOS all work.

Verify your toolchain with:

```shell
dotnet --version
```

## Building

The repository uses the modern XML solution format (`Brainfuck.Jinx.slnx`).

Build everything in Release:

```shell
dotnet build Brainfuck.Jinx.slnx -c Release
```

The build is strict by design: the core and shell projects enable
`TreatWarningsAsErrors`, nullable reference types, .NET analyzers, code-style
enforcement, and XML documentation generation. A clean build means zero
warnings and zero errors.

### Publishing a standalone shell

`Brainfuck.Shell` is configured for single-file publishing:

```shell
dotnet publish Brainfuck.Shell -c Release -r win-x64   --self-contained
dotnet publish Brainfuck.Shell -c Release -r linux-x64 --self-contained
dotnet publish Brainfuck.Shell -c Release -r osx-arm64 --self-contained
```

The resulting executable is a single file containing the runtime, the shell,
and the library.

## Running tests

Tests use **xUnit v3** on the **Microsoft.Testing.Platform** runner (configured
in `global.json`).

```shell
dotnet test Brainfuck.Jinx.slnx
```

Tests tagged `SpeedRegression` assert loose wall-clock budgets and are safe to
run anywhere; filter them out if you are on a heavily loaded machine:

```shell
dotnet test Brainfuck.Jinx.slnx -- --filter-not-trait "Category=SpeedRegression"
```

## Using the command-line shell

After building, run the shell through the SDK:

```shell
dotnet run --project Brainfuck.Shell -- <arguments>
```

…or use the published binary directly.

### Input sources

The program can be supplied in exactly one of three ways:

| Source | Syntax | Notes |
| ------ | ------ | ----- |
| Positional argument | `bf "++++++++[>++++++++<-]>."` | If the value names an existing file it is read as a file, otherwise it is treated as code. Use `-` to read the program from standard input. |
| File option | `bf --file program.b` / `bf -f program.b` | Explicitly read the program from a file. |
| Code option | `bf --code "+++++."` / `bf -c "+++++."` | Explicitly run an inline snippet. |

If no source is given, the shell prints its help text instead of blocking on
standard input. Supplying more than one source is an error.

### Executors

| Option | Default | Description |
| ------ | ------- | ----------- |
| `--jit` / `-j` | ✅ | Run with the IL-emitting JIT executor. |
| `--interpreter` / `-i` | | Run with the reference tree-walking interpreter. |

`--jit` and `--interpreter` are mutually exclusive. Both must produce identical
output; the flag exists to compare them and to debug.

### Inspecting optimized op-codes

Pass `--opcodes` / `-o` to **disassemble** the optimized program instead of
running it. This prints the op-code tree produced by `OptimizingParser`, with
loop bodies indented:

```shell
$ bf --code "+++++[->+>++<<]>>.<<." --opcodes
Add(5)
Mul(val=1 buf=1 off=0)
MulAndClear(val=2 buf=2 off=0)
Shift(2)
Write
Shift(-2)
Write
```

The multiply loop was folded into two arithmetic op-codes, and the pointer
moves were fused — exactly what the optimizer does before the executor sees the
program.

### Examples

```shell
# Classic "Hello World!" from an inline snippet.
bf -c "++++++++[>++++[>++>+++>+++>+<<<<-]>+>+>->>+[<]<-]>>.>---.+++++++..+++.>>.<-.<.+++.------.--------.>>+.>++."

# Run a bundled sample file.
bf samples/e.b

# Read the program from a pipe.
cat program.b | bf -

# Compare the two executors.
bf -i samples/squares.b
bf -j samples/squares.b
```

## Using the library

Add a reference to `Brainfuck.Jinx` and compose the pipeline yourself:

```csharp
using System.Text;
using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.IO;
using Brainfuck.Jinx.Lexer;
using Brainfuck.Jinx.Parser;

const string source = "++++++++[>++++++++<-]>.";

// 1. Lex: text -> instruction tokens (non-instruction characters are ignored).
IReadOnlyList<Token> tokens = new TextLexer().ParseTokens(source.AsSpan());

// 2. Parse: tokens -> optimized op-code tree.
List<OpCode> program = new OptimizingParser().Parse(tokens);

// 3. Execute against a fresh 30,000-cell tape.
using var io = new BufferedIo();
new JitExecutor().Execute(io, program);
```

Because the library exposes every stage, you can instead drive a machine whose
state survives between runs:

```csharp
using Brainfuck.Jinx.Machine;

using var io = new BufferedIo();
var machine = new FixedMachine(io);
var executor = new JitExecutor();

executor.Execute(machine, program);   // tape and pointer are preserved
executor.Execute(machine, program);   // …and reused here
```

`IMachineIo` is trivial to implement. A test-friendly implementation that
captures output to a list is fewer than twenty lines:

```csharp
sealed class CapturingIo(IEnumerable<byte>? input = null) : IMachineIo
{
    private readonly Queue<byte> _input = new(input ?? []);
    public List<byte> Output { get; } = [];

    public void Write(byte value) => Output.Add(value);
    public byte Read() => _input.Dequeue();
    public void Dispose() { }
}
```

(That is precisely the double used in `Brainfuck.Test/TestDoubles.cs`.)

## How it works

### Pipeline

```
                 TextLexer                SimpleParser
Brainfuck text ─────────────▶ Token[] ──────────────────┐
                                              │          │ (OptimizingParser
                                              │          │  adds patterns)
                                              ▼          ▼
                                          OpCode tree ───────────────▶ executor
                                                                          │
                                       InterpreterExecutor  ──┐           │
                                       JitExecutor          ──┤ IMachine / IMachineIo
                                       JitExecutor (inline) ──┘
```

Each stage is an interface (`ILexer`, `IParser`, `IExecutor`, `IMachine`,
`IMachineIo`), and each has one or more ready-to-use implementations.

### Lexer

`TextLexer` scans the source once. It maps the eight instruction characters to
the `Token` enum, counts lines and columns for error reporting, and validates
bracket balance. Every other character is a comment and is discarded, matching
Brainfuck's "ignore non-instructions" rule.

Two exceptions report ill-formed brackets:

- `UnmatchedClosingBracketException(line, character)` — a `]` with no `[`.
- `UnmatchedOpeningBracketException(count)` — one or more `[` left open at EOF.

### Parsers

`SimpleParser` (the base) lowers tokens into a flat op-code list and applies
only rewrites that are unconditionally safe:

- runs of `+`/`-` collapse into a single signed `Add`;
- runs of `>`/`<` collapse into a single signed `Shift`;
- `[ … ]` becomes a `Loop` node whose body is parsed recursively;
- an empty body becomes `Halt` (executing `[]` would hang);
- leading loops and halts are dropped, because the tape starts at zero.

`OptimizingParser` extends `SimpleParser` with structural rewrites. It runs the
pattern list over the tree repeatedly, bottom-up, until a full pass makes no
change.

### The optimizer

Every supported idiom is an `IPattern`. Patterns are offered **every** op-code
(not only loops), in order; the first match wins. A rule may consume a short run
of adjacent op-codes and replace it with any flat op-codes — including none, to
delete dead code.

To keep matching cheap, each loop body is summarised once into a `LoopAnalysis`
and that same value is handed to every pattern. A loop is *arithmetic* when its
body contains only `Add` and `Shift` and the pointer ends where it started; such
a loop has a closed form (a fixed multiple of the counter added to each
destination cell) and can be replaced without iterating.

| Pattern | Recognises | Produces |
| ------- | ---------- | -------- |
| `ZeroOpPattern` | `Add(0)` / `Shift(0)` (cancelled runs) | *deleted* |
| `ZeroLoopPattern` | `[-]` | `Set(0)` |
| `SetAddPattern` | `Set(x)` then `Add(y)` | `Set(x + y)` |
| `SetSetPattern` | `Set(x)` then `Set(y)` | `Set(y)` |
| `AddSetPattern` | `Add(y)` then `Set(x)` | `Set(x)` |
| `MulAndClearPattern` | `[->+<]` (one destination) | `MulAndClear` |
| `MulPattern` | `[->+>++<<]` (≥2 destinations) | `Mul*` + final `MulAndClear` |
| `PointerScanPattern` | `[>]`, `[<]`, `[>>]`, … | `PointerScan` |

Optimization is bottom-up and iterated to a fixed point, so a flattened inner
loop is visible when its parent is considered, and a rewrite that empties a
loop body is itself folded to `Halt`.

### Op-codes

`OpCode` is a `readonly record struct` with a `Type`, a `Value`, an optional
nested body (`OpCodes`, loops only), an `Offset`, and a `Buffer`. The lower half
of `OpCodeType` maps one-to-one onto Brainfuck's instructions; the upper half is
the fused, optimizer-produced vocabulary.

| Op-code | Meaning |
| ------- | ------- |
| `Add(value)` | `cell += value` (mod 256); a run of `+`/`-`. |
| `Shift(value)` | `position += value` (wrapping the tape); a run of `>`/`<`. |
| `Write` / `Read` | `.` / `,`. |
| `Loop(body)` | Repeat `body` while the current cell is non-zero. |
| `Set(value)` | `cell = value` (mod 256). |
| `Mul(value, buffer)` | `cell[position+buffer] += value * cell[position]`; pointer kept. |
| `MulAndClear(value, buffer)` | As `Mul`, then clear the source cell. |
| `PointerScan(value)` | While the current cell is non-zero, `position += value`. |
| `Halt` | Stop the program when the current cell is non-zero; a no-op on zero. Represents `[]`. |

All arithmetic is modulo 256, and all pointer movement wraps around the 30,000
cell tape.

### Machine model

`FixedMachine` is the standard `IMachine`: a zero-initialised `byte[30000]` with
a data pointer that always lies in `[0, 30000)`. `IMachine` exposes the same
operations the op-codes need (`Add`, `Shift`, `Write`, `Read`, `Set`, the `Mul`
family, `PointerScan`, `IsZero`). The tape is a ring, so tape-walking idioms that
never find a zero cell spin forever — matching a real fixed-tape machine.

### Executors

- **`InterpreterExecutor`** walks the op-code tree and dispatches each operation
  through `IMachine`. It is the behavioural reference.
- **`JitExecutor`** compiles the program into a `DynamicMethod` and invokes it.
  It has two paths:
  - `Execute(IMachine, …)` emits one interface call per op-code, preserving the
    caller's machine between runs;
  - `Execute(IMachineIo, …)` allocates the tape as a local `byte[]` and the
    pointer as a local `int`, so every cell access is a direct IL array access
    instead of a virtual dispatch. This is the fast path used by the shell.

The generated method is discarded after each call, so a program is recompiled
per execution. `JitInlineTests` asserts that interpreter, machine JIT, and
inlined JIT all produce identical output across a representative program set,
and guards tricky cases such as `Mul` offsets and `Halt`.

### I/O

`IMachineIo` is the program's `.`/`,` channel and is `IDisposable` so buffered
implementations can flush.

- **`SimpleIo`** writes each byte immediately with `Console.Write(char)`. Simple
  but slow for output-heavy programs.
- **`BufferedIo`** accumulates bytes and flushes to standard output on newline,
  before a read, and on disposal. It is line-buffered so prompts stay responsive.
  This is what the shell uses.

At end of input `Console.Read` returns `-1`, which the console I/O implementations
cast to `255` — a deliberate, documented choice.

## Repository layout

```
Brainfuck.Jinx.slnx            Modern XML solution file
global.json                    Test-runner configuration (Microsoft.Testing.Platform)
.editorconfig                  C# style, naming, and formatting rules

Brainfuck.Jinx/                Core library (net10.0)
├── Lexer/
│   ├── Token.cs               The eight instruction tokens
│   ├── ILexer.cs              Lexer abstraction
│   └── TextLexer.cs           Single-pass scanner + bracket errors
├── Parser/
│   ├── IParser.cs             Parser abstraction
│   ├── SimpleParser.cs        Base peephole parser
│   ├── OptimizingParser.cs    Pattern-driven optimizer
│   └── Patterns/              One file per rewrite rule + LoopAnalysis
├── Executor/
│   ├── OpCode.cs              Op-code type + record struct
│   ├── IExecutor.cs           Executor abstraction
│   ├── InterpreterExecutor.cs Reference tree-walking interpreter
│   └── JitExecutor.cs         IL-emitting JIT (machine + inlined paths)
├── Machine/
│   ├── IMachine.cs            Tape/pointer/I-O abstraction
│   └── FixedMachine.cs        30,000-cell ring tape
└── IO/
    ├── IMachineIo.cs          Byte I/O abstraction
    ├── SimpleIo.cs            Unbuffered console I/O
    └── BufferedIo.cs          Line-buffered console I/O

Brainfuck.Shell/               CLI entry point (System.CommandLine)
Brainfuck.Test/                xUnit v3 tests, test doubles, speed regressions
samples/                       Third-party Brainfuck programs (see below)
```

## Sample programs

`samples/` contains classic, non-trivial Brainfuck programs, used as manual
smoke tests and as realistic optimizer input:

| File | Program |
| ---- | ------- |
| `e.b` | Computes the digits of *e* (never terminates on its own). |
| `squares.b` | Prints square numbers up to 10,000. |
| `sierpinski.b` | Renders an ASCII Sierpinski triangle. |
| `mandelbrot.b` | Erik Bosman's Mandelbrot-set viewer (slow). |
| `chessboard.b` | Renders a chessboard from FEN input. |
| `bitwidth.b` | Probes cell width and interpreter correctness. |

These programs are third-party works (predominantly by Daniel B. Cristofani and
Erik Bosman) and carry their own terms — notably the Creative Commons
Attribution-ShareAlike 4.0 license. They are included for testing and
demonstration and are **not** covered by this project's MIT license. Check each
file's header before redistributing.

## License

The source code in this repository is released under the [MIT License](LICENSE).
The bundled programs under `samples/` are the work of their respective authors
and are governed by their own licenses.
