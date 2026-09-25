using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.Lexer;

namespace Brainfuck.Jinx.Parser;

public class SimpleParser: IParser
{
    public virtual List<OpCode> Parse(IReadOnlyList<Token> tokens)
    {
        var offset = 0;
        var result = Parse(tokens, ref offset);
        return result.SkipWhile(code => code.Type is OpCodeType.Loop).ToList();
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
                    var codes = Parse(tokens, ref offset);
                    if (codes.Count > 0)
                    {
                        result.Add(new OpCode(OpCodeType.Loop, 0, codes));
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
            Token.ShiftLeft => new OpCode(OpCodeType.Shift, -1),
            Token.ShiftRight => new OpCode(OpCodeType.Shift, 1),
            Token.Increment => new OpCode(OpCodeType.Add, 1),
            Token.Decrement => new OpCode(OpCodeType.Add, -1),
            Token.Write => new OpCode(OpCodeType.Write),
            Token.Read => new OpCode(OpCodeType.Read),
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, null)
        };
    }

    private static bool TryCombine(OpCode opCode1, OpCode opCode2, out OpCode result)
    {
        result = default;
        
        OpCode? r = (opCode1.Type, opCode2.Type) switch
        {
            (OpCodeType.Add, OpCodeType.Add) => new OpCode(OpCodeType.Add, opCode1.Value + opCode2.Value), 
            (OpCodeType.Shift, OpCodeType.Shift) => new OpCode(OpCodeType.Shift, opCode1.Value + opCode2.Value), 
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