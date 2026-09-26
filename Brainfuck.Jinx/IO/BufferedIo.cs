using System.Runtime.InteropServices;

namespace Brainfuck.Jinx.IO;

public sealed class BufferedIo: IMachineIo
{
    private readonly List<char> _buffer = [];

    public void Write(byte value)
    {
        var val = (char)value;
        _buffer.Add(val);
        if (val == '\n')
        {
            Flush();
        }
    }

    public byte Read()
    {
        Flush();
        return (byte)Console.Read();
    }

    private void Flush()
    {
        Console.Write(CollectionsMarshal.AsSpan(_buffer));
        _buffer.Clear();
    }
    
    public void Dispose()
    {
        Flush();
    }
}