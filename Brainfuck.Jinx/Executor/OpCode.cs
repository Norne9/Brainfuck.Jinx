namespace Brainfuck.Jinx.Executor;

public enum OpCodeType
{
    Add,
    Shift,
    Write,
    Read,
    Loop,
    SetZero
}

public readonly record struct OpCode(OpCodeType Type, int Value = 0, List<OpCode>? OpCodes = null)
{
    public override string ToString() =>
        this.Type switch
        {
            OpCodeType.Add => $"Add({Value})",
            OpCodeType.Shift => $"Shift({Value})",
            OpCodeType.Write => "Write",
            OpCodeType.Read => "Read",
            OpCodeType.Loop => "Loop[" + string.Join(", ", OpCodes ?? []) + "]",
            OpCodeType.SetZero => "SetZero",
            _ => throw new ArgumentOutOfRangeException()
        };
}