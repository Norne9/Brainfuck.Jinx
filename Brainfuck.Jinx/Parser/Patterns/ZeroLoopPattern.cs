using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

public class ZeroLoopPattern: IPattern
{
    public (int count, List<OpCode>? newCodes) Apply(List<OpCode> opcodes, int offset)
    {
        var code = opcodes[offset];
        if (code is { Type: OpCodeType.Loop, OpCodes: [{ Type: OpCodeType.Add, Value: -1 }] })
        {
            return (1, [new OpCode(OpCodeType.SetZero)]);
        }
        return (0, null);
    }
}