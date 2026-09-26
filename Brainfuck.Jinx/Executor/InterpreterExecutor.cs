using Brainfuck.Jinx.IO;
using Brainfuck.Jinx.Machine;

namespace Brainfuck.Jinx.Executor;

/// <summary>
/// The reference executor: a tree-walking interpreter that dispatches every
/// <see cref="OpCode"/> through the <see cref="IMachine"/> interface.
/// </summary>
/// <remarks>
/// This implementation favours clarity over speed and is the behavioural
/// baseline that <see cref="JitExecutor"/> is tested against. Loops are executed
/// by recursively re-entering <see cref="Execute(IMachine, IReadOnlyList{OpCode})"/>
/// for the loop body.
/// </remarks>
public class InterpreterExecutor : IExecutor
{
    /// <summary>
    /// Runs <paramref name="opcodes"/> against <paramref name="machine"/> in
    /// order, recursing into loop bodies as it meets them.
    /// </summary>
    /// <param name="machine">The machine the program mutates.</param>
    /// <param name="opcodes">The program (or loop body) to run.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when an op-code has a <see cref="OpCodeType"/> this executor does
    /// not know about.
    /// </exception>
    public void Execute(IMachine machine, IReadOnlyList<OpCode> opcodes)
    {
        foreach (var opcode in opcodes)
        {
            switch (opcode.Type)
            {
                case OpCodeType.Add:
                    machine.Add(opcode.Value);
                    break;
                case OpCodeType.Shift:
                    machine.Shift(opcode.Value);
                    break;
                case OpCodeType.Write:
                    machine.Write();
                    break;
                case OpCodeType.Read:
                    machine.Read();
                    break;
                case OpCodeType.Loop:
                    // The parser only emits Loop for a non-empty body; an empty
                    // body is lowered to Halt instead.
                    while (!machine.IsZero())
                    {
                        Execute(machine, opcode.OpCodes!);
                    }
                    break;
                case OpCodeType.Set:
                    machine.Set(opcode.Value);
                    break;
                case OpCodeType.Mul:
                    machine.Mul(opcode.Value, opcode.Buffer);
                    Shift(machine, opcode.Offset);
                    break;
                case OpCodeType.MulAndClear:
                    machine.MulAndClear(opcode.Value, opcode.Buffer);
                    Shift(machine, opcode.Offset);
                    break;
                case OpCodeType.PointerScan:
                    machine.PointerScan(opcode.Value);
                    break;
                case OpCodeType.Halt:
                    // `[]` would loop forever; the parser lowers it to Halt, which
                    // stops the program only when the current cell is non-zero.
                    if (!machine.IsZero())
                    {
                        return;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(opcodes), opcode.Type,
                        $"Unknown OpCode: {opcode.Type}");
            }
        }
    }

    /// <summary>
    /// Executes <paramref name="opcodes"/> on a fresh <see cref="FixedMachine"/>
    /// that reads and writes through <paramref name="io"/>.
    /// </summary>
    /// <param name="io">The input/output channel the program uses.</param>
    /// <param name="opcodes">The parsed program to run.</param>
    public void Execute(IMachineIo io, IReadOnlyList<OpCode> opcodes)
    {
        var machine = new FixedMachine(io);
        Execute(machine, opcodes);
    }

    /// <summary>
    /// Performs the pointer move that follows a <c>Mul</c>-family op-code,
    /// skipping the call when the offset is zero.
    /// </summary>
    /// <param name="machine">The machine to shift.</param>
    /// <param name="offset">The relative pointer movement; zero means "do nothing".</param>
    private static void Shift(IMachine machine, int offset)
    {
        if (offset != 0)
        {
            machine.Shift(offset);
        }
    }
}
