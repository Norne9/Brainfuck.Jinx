using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.Lexer;
using Brainfuck.Jinx.Parser.Patterns;

namespace Brainfuck.Jinx.Parser;

public class OptimizingParser: SimpleParser
{
    private readonly IPattern[] _patterns =
    [
        new ZeroLoopPattern(),
        new MulAndClearPattern(),
        new MulPattern(),
        new MulAndMulPattern()
    ];
    
    public override List<OpCode> Parse(IReadOnlyList<Token> tokens)
    {
        var opCodes = base.Parse(tokens).ToList();
        while (true)
        {
            if (!Optimize(opCodes))
            {
                break;
            }
        }
        return opCodes;
    }

    private bool Optimize(List<OpCode> opCodes)
    {
        var madeChanges = false;
        for (var i = opCodes.Count - 1; i >= 0; i--)
        {
            foreach (var pattern in _patterns)
            {
                (int remove, List<OpCode>? newCodes) = pattern.Apply(opCodes, i);
                if (remove > 0) {
                    opCodes.RemoveRange(i, remove);
                    madeChanges = true;
                }
                if (newCodes is not null)
                {
                    opCodes.InsertRange(i, newCodes);
                    madeChanges = true;
                }
            }

            if (opCodes[i].Type == OpCodeType.Loop && opCodes[i].OpCodes?.Count > 0)
            {
                var codes = opCodes[i].OpCodes!;
                madeChanges |= Optimize(codes);
                opCodes[i] = new OpCode(OpCodeType.Loop, 0, codes);
            }
        }
        return madeChanges;
    }
}