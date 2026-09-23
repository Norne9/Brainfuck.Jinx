namespace Brainfuck.Jinx.Executor;

public union OpCode(OpCode.Add, OpCode.Shift, OpCode.Write, OpCode.Read, OpCode.Loop)
{
    public readonly record struct Add(int value);
    public readonly record struct Shift(int value);
    public readonly record struct Write;
    public readonly record struct Read;
    public readonly record struct Loop(IReadOnlyList<OpCode> opCodes)
    {
        public override string ToString() {
            return "Loop[" + string.Join(", ", opCodes) + "]";
        }
    }

    public override string ToString()
    {
        return this switch
        {
            OpCode.Add a => $"Add({a.value})",
            OpCode.Shift s => $"Shift({s.value})",
            OpCode.Write => "Write",
            OpCode.Read => "Read",
            OpCode.Loop l => l.ToString()
        };
    }
}