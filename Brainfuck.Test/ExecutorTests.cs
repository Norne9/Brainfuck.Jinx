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
            new OpCode.Add(3),
            new OpCode.Shift(-2),
            new OpCode.Read(),
            new OpCode.Write()
        ];

        _executor.Execute(machine, opCodes);

        Assert.Equal(["Add(3)", "Shift(-2)", "Read", "Write"], machine.Operations);
    }

    [Fact]
    public void Execute_SkipsLoopWhenCurrentCellIsZero()
    {
        var machine = new RecordingMachine();
        OpCode[] opCodes = [new OpCode.Loop([new OpCode.Write()])];

        _executor.Execute(machine, opCodes);

        Assert.Empty(machine.Operations);
    }

    [Fact]
    public void Execute_RepeatsLoopUntilCurrentCellIsZero()
    {
        var machine = new RecordingMachine(3);
        OpCode[] opCodes = [new OpCode.Loop([new OpCode.Write(), new OpCode.Add(-1)])];

        _executor.Execute(machine, opCodes);

        Assert.Equal(
        [
            "Write", "Add(-1)",
            "Write", "Add(-1)",
            "Write", "Add(-1)"
        ], machine.Operations);
    }
}
