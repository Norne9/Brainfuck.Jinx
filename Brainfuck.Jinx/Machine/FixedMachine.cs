using Brainfuck.Jinx.IO;

namespace Brainfuck.Jinx.Machine;

/// <summary>
/// The standard <see cref="IMachine"/>: a zero-initialised, fixed 30,000-byte
/// tape on a ring, with the data pointer and I/O channel supplied by the caller.
/// </summary>
/// <param name="io">The input/output channel commands are routed through.</param>
/// <remarks>
/// Because the tape is a ring, shifting past either end wraps around and loop
/// idioms that walk forever on non-zero cells (such as <c>[&gt;]</c> on a fully
/// non-zero tape) spin indefinitely, matching a real fixed-tape Brainfuck
/// machine. All arithmetic is modulo 256.
/// </remarks>
public class FixedMachine(IMachineIo io) : IMachine
{
    /// <summary>The fixed tape length, in bytes.</summary>
    private const int MemorySize = 30_000;

    /// <summary>The tape, zero-filled on construction.</summary>
    private readonly byte[] _memory = new byte[MemorySize];

    /// <summary>The index of the current cell; always in <c>[0, MemorySize)</c>.</summary>
    private int _position;

    /// <inheritdoc />
    public void Add(int value)
    {
        _memory[_position] = (byte)(_memory[_position] + value);
    }

    /// <inheritdoc />
    public void Shift(int value)
    {
        var offset = value % MemorySize; // in (-30000, 30000)
        _position = (_position + offset + MemorySize) % MemorySize;
    }

    /// <inheritdoc />
    public void Write()
    {
        io.Write(_memory[_position]);
    }

    /// <inheritdoc />
    public void Read()
    {
        _memory[_position] = io.Read();
    }

    /// <inheritdoc />
    public void Set(int value)
    {
        _memory[_position] = (byte)value;
    }

    /// <inheritdoc />
    public void Mul(int value, int buffer)
    {
        var destination = Wrap(_position + buffer);
        _memory[destination] = (byte)(_memory[destination] + value * _memory[_position]);
    }

    /// <inheritdoc />
    public void MulAndClear(int value, int buffer)
    {
        var destination = Wrap(_position + buffer);
        _memory[destination] = (byte)(_memory[destination] + value * _memory[_position]);
        _memory[_position] = 0;
    }

    /// <inheritdoc />
    public void MulAndMul(int value, int buffer)
    {
        var destination = Wrap(_position + buffer);
        _memory[destination] = (byte)(_memory[destination] * value * _memory[_position]);
    }

    /// <inheritdoc />
    public void PointerScan(int direction)
    {
        // Brainfuck's `[>]`/`[<]` idiom: advance until the current cell is zero.
        // The tape is a fixed ring, so a tape that is non-zero all the way
        // around spins forever -- exactly as the equivalent generic loop would.
        while (_memory[_position] != 0)
        {
            Shift(direction);
        }
    }

    /// <inheritdoc />
    public bool IsZero() => _memory[_position] == 0;

    /// <summary>
    /// Wraps an arbitrary cell index onto the tape ring.
    /// </summary>
    /// <param name="position">The unwrapped index, possibly negative.</param>
    /// <returns>The equivalent index in <c>[0, MemorySize)</c>.</returns>
    private static int Wrap(int position)
    {
        var offset = position % MemorySize; // in (-30000, 30000)
        return (offset + MemorySize) % MemorySize;
    }
}
