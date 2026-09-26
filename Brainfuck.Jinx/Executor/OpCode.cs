namespace Brainfuck.Jinx.Executor;

public enum OpCodeType
{
    Add,
    Shift,
    Write,
    Read,
    Loop,
    Set,
    Mul,
    MulAndClear,
    MulAndMul,
    PointerScan,
    Halt
}

public readonly record struct OpCode(
    OpCodeType Type,
    int Value = 0,
    List<OpCode>? OpCodes = null,
    int Offset = 0,
    int Buffer = 0)
{
    public override string ToString() =>
        this.Type switch
        {
            OpCodeType.Add => $"Add({Value})",
            OpCodeType.Shift => $"Shift({Value})",
            OpCodeType.Write => "Write",
            OpCodeType.Read => "Read",
            OpCodeType.Loop => "Loop[" + string.Join(", ", OpCodes ?? []) + "]",
            OpCodeType.Set => $"Set({Value})",
            OpCodeType.Mul => $"Mul(val={Value} buf={Buffer} off={Offset})",
            OpCodeType.MulAndClear => $"MulAndClear(val={Value} buf={Buffer} off={Offset})",
            OpCodeType.MulAndMul => $"MulAndMul(val={Value} buf={Buffer} off={Offset})",
            OpCodeType.PointerScan => $"PointerScan({Value})",
            OpCodeType.Halt => "Halt",
            _ => throw new ArgumentOutOfRangeException()
        };
}