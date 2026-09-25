using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

// [->+>++<<] => Mul(val=1 buf=1 off=0) + MulAndClear(val=2 buf=2 off=0)
public class MulPattern : IPattern
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

        if (counterDelta != -1 || destinations.Count < 2)
        {
            return (0, null);
        }

        var newCodes = new List<OpCode>(destinations.Count);
        for (var i = 0; i < destinations.Count; i++)
        {
            var (buffer, value) = destinations[i];
            var type = i == destinations.Count - 1 ? OpCodeType.MulAndClear : OpCodeType.Mul;
            newCodes.Add(new OpCode(type, value, null, 0, buffer));
        }

        return (1, newCodes);
    }
}
