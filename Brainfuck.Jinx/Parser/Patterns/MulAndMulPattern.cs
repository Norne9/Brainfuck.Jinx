using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

// A loop whose body already contains a flattened inner loop, e.g.
// [->[->+<]<] => MulAndMul(val=1 off=1 buf=1)
//                 + MulAndClear(val=1 off=-1 buf=1)
//                 + SetZero
public class MulAndMulPattern : IPattern
{
    public (int count, List<OpCode>? newCodes) Apply(List<OpCode> opcodes, int offset)
    {
        var code = opcodes[offset];
        if (code.Type != OpCodeType.Loop || code.OpCodes is null)
        {
            return (0, null);
        }

        var pointer = 0;
        var counterDelta = 0;
        OpCode? flattened = null;
        var flattenedAt = 0;
        foreach (var op in code.OpCodes)
        {
            switch (op.Type)
            {
                case OpCodeType.Shift:
                    pointer += op.Value;
                    break;
                case OpCodeType.Add when pointer == 0:
                    counterDelta += op.Value;
                    break;
                case OpCodeType.Mul or OpCodeType.MulAndClear when flattened is null && op.Offset == 0:
                    flattened = op;
                    flattenedAt = pointer;
                    break;
                default:
                    return (0, null);
            }
        }

        if (flattened is not { } inner || pointer != 0 || counterDelta != -1)
        {
            return (0, null);
        }

        return (1,
        [
            new OpCode(OpCodeType.MulAndMul, 1, null, flattenedAt, flattenedAt),
            inner with { Offset = -flattenedAt },
            new OpCode(OpCodeType.SetZero)
        ]);
    }
}
