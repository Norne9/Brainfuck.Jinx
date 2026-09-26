using Brainfuck.Jinx.IO;
using Brainfuck.Jinx.Machine;

namespace Brainfuck.Jinx.Executor;

public class InterpreterExecutor : IExecutor
{
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
                case OpCodeType.MulAndMul:
                    machine.MulAndMul(opcode.Value, opcode.Buffer);
                    Shift(machine, opcode.Offset);
                    break;
                case OpCodeType.PointerScan:
                    machine.PointerScan(opcode.Value);
                    break;
                case OpCodeType.Halt:
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

    public void Execute(IMachineIo io, IReadOnlyList<OpCode> opcodes)
    {
        var machine = new FixedMachine(io);
        Execute(machine, opcodes);
    }

    private static void Shift(IMachine machine, int offset)
    {
        if (offset != 0)
        {
            machine.Shift(offset);
        }
    }
}