using Brainfuck.Jinx.Machine;

namespace Brainfuck.Jinx.Executor;

public class InterpreterExecutor: IExecutor
{
    public void Execute(IMachine machine, IReadOnlyList<OpCode> opcodes)
    {
        foreach (var opcode in opcodes)
        {
            switch (opcode)
            {
                case OpCode.Add add:
                    machine.Add(add.value);
                    break;
                case OpCode.Shift shift:
                    machine.Shift(shift.value);
                    break;
                case OpCode.Read:
                    machine.Read();
                    break;
                case OpCode.Write:
                    machine.Write();
                    break;
                case OpCode.Loop loop:
                    while (!machine.IsZero())
                    {
                        Execute(machine, loop.opCodes);
                    }
                    break;
            }
        }
    }
}