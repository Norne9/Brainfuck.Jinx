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
                case OpCodeType.SetZero:
                    machine.SetZero();
                    break;
                case OpCodeType.Mul:
                    machine.Mul(opcode.Value, opcode.Buffer);
                    Shift(machine, opcode.Offset);
                    break;
                case OpCodeType.MulAndClear:
                    machine.MulAndClear(opcode.Value, opcode.Buffer);
                    Shift(machine, opcode.Offset);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(opcodes), opcode.Type,
                        $"Unknown OpCode: {opcode.Type}");
            }
        }
    }

    private static void Shift(IMachine machine, int offset)
    {
        if (offset != 0)
        {
            machine.Shift(offset);
        }
    }
}