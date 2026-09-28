using Brainfuck.Jinx.IO;
using Brainfuck.Jinx.Machine;

namespace Brainfuck.Test;

internal sealed class BufferedIo(IEnumerable<byte>? input = null) : IMachineIo
{
    private readonly Queue<byte> _input = new(input ?? []);

    public List<byte> Output { get; } = [];

    public void Write(byte value) => Output.Add(value);

    public byte Read() => _input.Dequeue();

    public void Dispose()
    {
    }
}

internal sealed class RecordingMachine(byte initialValue = 0) : IMachine
{
    private byte _value = initialValue;

    public List<string> Operations { get; } = [];

    public void Add(int value)
    {
        Operations.Add($"Add({value})");
        _value = (byte)(_value + value);
    }

    public void Shift(int value) => Operations.Add($"Shift({value})");

    public void Write() => Operations.Add("Write");

    public void Read() => Operations.Add("Read");

    public void Set(int value)
    {
        Operations.Add($"Set({value})");
        _value = (byte)value;
    }

    public void Mul(int value, int buffer) =>
        Operations.Add($"Mul({value}, {buffer})");

    public void MulAndClear(int value, int buffer) =>
        Operations.Add($"MulAndClear({value}, {buffer})");

    public void MulAndMul(int value, int buffer) =>
        Operations.Add($"MulAndMul({value}, {buffer})");

    public void PointerScan(int direction) =>
        Operations.Add($"PointerScan({direction})");

    public bool IsZero() => _value == 0;
}
