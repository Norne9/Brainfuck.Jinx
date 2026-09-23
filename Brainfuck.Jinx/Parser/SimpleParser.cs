using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.Lexer;

namespace Brainfuck.Jinx.Parser;

public class SimpleParser: IParser
{
    public IReadOnlyList<OpCode> Parse(IReadOnlyList<Token> tokens)
    {
        var offset = 0;
        var result = Parse(tokens, ref offset);
        return result.SkipWhile(code => code is OpCode.Loop).ToList();
    }
    
    private static List<OpCode> Parse(IReadOnlyList<Token> tokens, ref int offset)
    {
        var result = new List<OpCode>();
        while (offset < tokens.Count)
        {
            var token = tokens[offset++];
            switch (token)
            {
                case Token.LeftBracket:
                    var loop = new OpCode.Loop(Parse(tokens, ref offset));
                    if (loop.opCodes.Count > 0)
                    {
                        result.Add(loop);
                    }
                    break;
                case Token.RightBracket:
                    return result;
                default:
                    var opCode = ToOpCode(token);
                    if (result.Count > 0 && TryCombine(opCode, result[^1], out var code))
                    {
                        result[^1] = code;
                    }
                    else
                    {
                        result.Add(opCode);
                    }
                    break;
            }
        }
        
        return result;
    }

    private static OpCode ToOpCode(Token token)
    {
        return token switch
        {
            Token.ShiftLeft => new OpCode.Shift(-1),
            Token.ShiftRight => new OpCode.Shift(1),
            Token.Increment => new OpCode.Add(1),
            Token.Decrement => new OpCode.Add(-1),
            Token.Write => new OpCode.Write(),
            Token.Read => new OpCode.Read(),
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, null)
        };
    }

    private static bool TryCombine(OpCode opCode1, OpCode opCode2, out OpCode result)
    {
        result = default;
        
        OpCode? r = (opCode1, opCode2) switch
        {
            (OpCode.Add x, OpCode.Add y) => new OpCode.Add(x.value + y.value), 
            (OpCode.Shift x, OpCode.Shift y) => new OpCode.Shift(x.value + y.value), 
            _ => null
        };
        
        if (!r.HasValue)
        {
            return false;
        }
        
        result = r.Value;
        return true;
    }
}