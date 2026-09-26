using System.Runtime.InteropServices;

namespace Brainfuck.Jinx.IO;

/// <summary>
/// A line-buffered <see cref="IMachineIo"/> that accumulates output in memory
/// and flushes it to <see cref="Console"/> when a newline is written, before a
/// read, or on <see cref="Dispose"/>.
/// </summary>
/// <remarks>
/// Brainfuck programs often emit many single-byte writes; batching them into one
/// <see cref="Console.Write(ReadOnlySpan{char})"/> per line avoids the per-write
/// console overhead. Flushing before a read keeps prompt-like programs
/// responsive.
/// </remarks>
public sealed class BufferedIo: IMachineIo
{
    /// <summary>Pending output, as characters, not yet written to the console.</summary>
    private readonly List<char> _buffer = [];

    /// <inheritdoc />
    public void Write(byte value)
    {
        var val = (char)value;
        _buffer.Add(val);
        if (val == '\n')
        {
            Flush();
        }
    }

    /// <inheritdoc />
    public byte Read()
    {
        // Show everything written so far before blocking on input, so the user
        // can see a prompt that has not been terminated by a newline.
        Flush();
        return (byte)Console.Read();
    }

    /// <summary>
    /// Writes all buffered characters to the console and clears the buffer.
    /// </summary>
    private void Flush()
    {
        Console.Write(CollectionsMarshal.AsSpan(_buffer));
        _buffer.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Flush();
    }
}
