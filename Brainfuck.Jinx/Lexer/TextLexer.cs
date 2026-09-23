namespace Brainfuck.Jinx.Lexer;

public class TextLexer: ILexer
{
    public IReadOnlyList<Token> ParseTokens(ReadOnlySpan<char> input)
    {
        var result = new List<Token>();
        var bracketWatcher = 0;
        var lines = 1;
        var characters = 0;
        foreach (var token in input)
        {
            characters += 1;
            switch (token)
            {
                case '>':
                    result.Add(Token.ShiftRight);
                    break;
                case '<':
                    result.Add(Token.ShiftLeft);
                    break;
                case '.':
                    result.Add(Token.Write);
                    break;
                case ',':
                    result.Add(Token.Read);
                    break;
                case '+':
                    result.Add(Token.Increment);
                    break;
                case '-':
                    result.Add(Token.Decrement);
                    break;
                case '[':
                    bracketWatcher += 1;
                    result.Add(Token.LeftBracket);
                    break;
                case ']':
                    bracketWatcher -= 1;
                    if (bracketWatcher < 0)
                    {
                        throw new UnmatchedOpeningBracketException(lines, characters);
                    }
                    result.Add(Token.RightBracket);
                    break;
                case '\n':
                    lines += 1;
                    characters = 0;
                    break;
            }
        }

        if (bracketWatcher > 0)
        {
            throw new UnmatchedClosingBracketException(bracketWatcher);
        }
        return result;
    }

    public class UnmatchedOpeningBracketException(int line, int character) :
        Exception($"Unmatched ']' at position ({line}:{character})");
    
    public class UnmatchedClosingBracketException(int count) : Exception($"Found {count} unmatched '['");
}