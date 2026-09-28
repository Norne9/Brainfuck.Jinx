using Brainfuck.Jinx.Executor;
using Brainfuck.Jinx.Lexer;

namespace Brainfuck.Jinx.Parser;

/// <summary>
/// The base parser: turns tokens into op-codes and applies the two trivially
/// safe peephole rewrites, plus a start-of-program cleanup.
/// </summary>
/// <remarks>
/// <para>
/// The rewrites are:
/// </para>
/// <list type="bullet">
///   <item><description>
///     adjacent <c>+</c>/<c>-</c> tokens combine into one signed
///     <see cref="OpCodeType.Add"/>, and adjacent <c>&gt;</c>/<c>&lt;</c> tokens
///     into one signed <see cref="OpCodeType.Shift"/>;
///   </description></item>
///   <item><description>
///     a <c>[ ... ]</c> block becomes an <see cref="OpCodeType.Loop"/> whose
///     <see cref="OpCode.OpCodes"/> are the parsed body, or an
///     <see cref="OpCodeType.Halt"/> when the body is empty.
///   </description></item>
/// </list>
/// <para>
/// Leading loops are dropped: the tape is zero-filled at program start, so the
/// current cell is zero and a loop reached before any instruction can modify it
/// never executes. Dropping it (together with any leading <see cref="OpCodeType.Halt"/>,
/// which is a no-op on a zero cell) keeps the op-code stream minimal.
/// </para>
/// </remarks>
public class SimpleParser: IParser
{
    /// <summary>
    /// Parses <paramref name="tokens"/> into a top-level op-code list, removing
    /// the leading loops and halts that can never execute.
    /// </summary>
    /// <param name="tokens">The token stream produced by an <see cref="ILexer"/>.</param>
    /// <returns>The parsed program.</returns>
    public virtual List<OpCode> Parse(IReadOnlyList<Token> tokens)
    {
        var offset = 0;
        var result = Parse(tokens, ref offset);

        // The tape starts zeroed, so any loop encountered at the top level before
        // the first pointer/cell mutation is skipped, and a leading Halt (empty
        // loop) does nothing. SkipWhile discards that dead prefix. This applies
        // only to the top level: a loop body begins with a non-zero cell (that is
        // why the loop is entered), so a leading loop inside a body is live.
        return result.SkipWhile(code => code.Type is OpCodeType.Loop or OpCodeType.Halt).ToList();
    }

    /// <summary>
    /// Recursively parses the token run starting at <paramref name="offset"/>,
    /// stopping when the closing bracket of the current loop is reached (or the
    /// input ends).
    /// </summary>
    /// <param name="tokens">The full token stream.</param>
    /// <param name="offset">
    /// The cursor into <paramref name="tokens"/>; advanced as tokens are consumed.
    /// </param>
    /// <returns>The op-codes of this run or loop body.</returns>
    private static List<OpCode> Parse(IReadOnlyList<Token> tokens, ref int offset)
    {
        var result = new List<OpCode>();
        while (offset < tokens.Count)
        {
            var token = tokens[offset++];
            switch (token)
            {
                case Token.LeftBracket:
                    // Parse the body, then wrap it. An empty body would otherwise
                    // be an infinite no-op loop, so represent it as Halt instead.
                    var codes = Parse(tokens, ref offset);
                    result.Add(codes.Count > 0 ? new OpCode(OpCodeType.Loop, 0, codes) : new OpCode(OpCodeType.Halt));
                    break;
                case Token.RightBracket:
                    // End of the current loop body; the caller's Parse owns the
                    // matching LeftBracket.
                    return result;
                default:
                    var opCode = ToOpCode(token);
                    // Fold the new op-code into the previous one when they are
                    // the same arithmetic kind.
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

    /// <summary>
    /// Maps a single non-bracket token to the corresponding op-code.
    /// </summary>
    /// <param name="token">The token to translate.</param>
    /// <returns>The matching op-code.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown for the bracket tokens, which are handled structurally rather than
    /// here.
    /// </exception>
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

    /// <summary>
    /// Tries to merge two adjacent arithmetic op-codes into one.
    /// </summary>
    /// <param name="opCode1">The newer op-code.</param>
    /// <param name="opCode2">The already-parsed predecessor.</param>
    /// <param name="result">
    /// On success, the merged op-code; otherwise <see langword="default"/>.
    /// </param>
    /// <returns><see langword="true"/> when the two op-codes were merged.</returns>
    /// <remarks>
    /// Only <see cref="OpCodeType.Add"/> and <see cref="OpCodeType.Shift"/> merge,
    /// and only with their own kind; the addition of their values is commutative,
    /// so argument order does not matter.
    /// </remarks>
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
