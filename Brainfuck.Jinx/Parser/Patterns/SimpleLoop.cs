using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

// Analyses a flat loop body (only cell arithmetic and pointer moves) and
// reports the net per-iteration delta of every cell relative to the counter.
internal static class SimpleLoop
{
    public static bool TryAnalyze(
        IReadOnlyList<OpCode> body,
        out int counterDelta,
        out List<(int Buffer, int Value)> destinations)
    {
        counterDelta = 0;
        destinations = [];

        var deltas = new Dictionary<int, int>();
        var pointer = 0;
        foreach (var op in body)
        {
            switch (op.Type)
            {
                case OpCodeType.Shift:
                    pointer += op.Value;
                    break;
                case OpCodeType.Add:
                    deltas[pointer] = deltas.GetValueOrDefault(pointer) + op.Value;
                    break;
                default:
                    return false;
            }
        }

        if (pointer != 0)
        {
            return false;
        }

        counterDelta = deltas.GetValueOrDefault(0);
        foreach (var (offset, value) in deltas.OrderBy(pair => pair.Key))
        {
            if (offset != 0 && value != 0)
            {
                destinations.Add((offset, value));
            }
        }

        return true;
    }
}
