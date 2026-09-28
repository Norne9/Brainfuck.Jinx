namespace Brainfuck.Jinx.IO;

/// <summary>
/// The simplest <see cref="IMachineIo"/>: writes straight to
/// <see cref="Console"/> with no buffering.
/// </summary>
/// <remarks>
/// Each <see cref="Write"/> becomes an immediate <see cref="Console.Write(char)"/>
/// call, which can be slow for output-heavy programs. Prefer <see cref="BufferedIo"/>
/// when speed matters. <see cref="Read"/> returns the raw result of
/// <see cref="Console.Read"/> cast to a byte, so end of input (which
/// <see cref="Console.Read"/> reports as -1) becomes 255.
/// </remarks>
public class SimpleIo: IMachineIo
{
    /// <inheritdoc />
    public void Write(byte value) => Console.Write((char)value);

    /// <inheritdoc />
    public byte Read() => (byte)Console.Read();

    /// <inheritdoc />
    public void Dispose()
    {
        // Console needs no flushing; provided to satisfy IDisposable.
    }
}
