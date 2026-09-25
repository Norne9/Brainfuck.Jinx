using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

// [->+<] => MulAndClear(val=1 buf=1 off=0)
public class MulAndClearPattern : IPattern
{
    public (int count, List<OpCode>? newCodes) Apply(List<OpCode> opcodes, int offset)
    {
        var code = opcodes[offset];
        if (code.Type != OpCodeType.Loop || code.OpCodes is null)
        {
            return (0, null);
        }

        if (!SimpleLoop.TryAnalyze(code.OpCodes, out var counterDelta, out var destinations))
        {
            return (0, null);
        }

        if (counterDelta != -1 || destinations.Count != 1)
        {
            return (0, null);
        }

        var (buffer, value) = destinations[0];
        return (1, [new OpCode(OpCodeType.MulAndClear, value, null, 0, buffer)]);
    }
}
