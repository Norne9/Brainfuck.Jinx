namespace Brainfuck.Jinx.IO;

/// <summary>
/// The input/output channel a Brainfuck program uses for its <c>.</c> and
/// <c>,</c> instructions.
/// </summary>
/// <remarks>
/// Implementations are free to buffer output for speed; because the channel owns
/// that buffer, callers should dispose the implementation so any pending output
/// is flushed.
/// </remarks>
public interface IMachineIo: IDisposable
{
    /// <summary>
    /// Writes one byte to the output.
    /// </summary>
    /// <param name="value">The byte to write.</param>
    void Write(byte value);

    /// <summary>
    /// Reads one byte from the input.
    /// </summary>
    /// <returns>The byte read; implementations decide what to return at end of input.</returns>
    byte Read();
}
