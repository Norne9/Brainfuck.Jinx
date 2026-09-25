using Brainfuck.Jinx.Executor;

namespace Brainfuck.Test;

public class ExecutorTests
{
    private readonly InterpreterExecutor _executor = new();

    [Fact]
    public void Execute_DispatchesEachBasicOpCodeInOrder()
    {
        var machine = new RecordingMachine();
        OpCode[] opCodes =
        [
            new (OpCodeType.Add,3),
            new (OpCodeType.Shift,-2),
            new (OpCodeType.Read),
            new (OpCodeType.Write)
        ];

        _executor.Execute(machine, opCodes);

        Assert.Equal(["Add(3)", "Shift(-2)", "Read", "Write"], machine.Operations);
    }

    [Fact]
    public void Execute_SkipsLoopWhenCurrentCellIsZero()
    {
        var machine = new RecordingMachine();
        OpCode[] opCodes = [new (OpCodeType.Loop,0,[new OpCode(OpCodeType.Write)])];

        _executor.Execute(machine, opCodes);

        Assert.Empty(machine.Operations);
    }

    [Fact]
    public void Execute_RepeatsLoopUntilCurrentCellIsZero()
    {
        var machine = new RecordingMachine(3);
        OpCode[] opCodes = [new (OpCodeType.Loop,0,[
            new OpCode(OpCodeType.Write),
            new OpCode(OpCodeType.Add, -1)
        ])];

        _executor.Execute(machine, opCodes);

        Assert.Equal(
        [
            "Write", "Add(-1)",
            "Write", "Add(-1)",
            "Write", "Add(-1)"
        ], machine.Operations);
    }
}
