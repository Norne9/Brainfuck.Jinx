using Brainfuck.Jinx.IO;

namespace Brainfuck.Jinx.Machine;

public class FixedMachine(IMachineIo io) : IMachine
{
    private const int MemorySize = 30_000;
    private readonly byte[] _memory = new byte[MemorySize];
    
    private int _position;
    
    public void Add(int value)
    {
        _memory[_position] = (byte)(_memory[_position] + value);
    }

    public void Shift(int value)
    {
        var offset = value % MemorySize; // in (-30000, 30000)
        _position = (_position + offset + MemorySize) % MemorySize;
    }

    public void Write()
    {
        io.Write(_memory[_position]);
    }

    public void Read()
    {
        _memory[_position] = io.Read();
    }

    public void SetZero()
    {
        _memory[_position] = 0;
    }

    public void MulAndClear(int value, int buffer)
    {
        var destination = Wrap(_position + buffer);
        _memory[destination] = (byte)(_memory[destination] + value * _memory[_position]);
        _memory[_position] = 0;
    }

    public bool IsZero() => _memory[_position] == 0;

    private static int Wrap(int position)
    {
        var offset = position % MemorySize; // in (-30000, 30000)
        return (offset + MemorySize) % MemorySize;
    }
}