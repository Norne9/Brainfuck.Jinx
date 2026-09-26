using Brainfuck.Jinx.Machine;

namespace Brainfuck.Test;

public class MachineTests
{
    [Fact]
    public void Add_WrapsByteInBothDirections()
    {
        using var io = new BufferedIo();
        var machine = new FixedMachine(io);

        machine.Add(-1);
        machine.Write();
        machine.Add(2);
        machine.Write();

        Assert.Equal([byte.MaxValue, (byte)1], io.Output);
    }

    [Fact]
    public void Shift_SelectsIndependentCells()
    {
        using var io = new BufferedIo();
        var machine = new FixedMachine(io);

        machine.Add(10);
        machine.Shift(1);
        machine.Add(20);
        machine.Write();
        machine.Shift(-1);
        machine.Write();

        Assert.Equal([(byte)20, (byte)10], io.Output);
    }

    [Fact]
    public void Shift_WrapsAroundMemoryInBothDirections()
    {
        using var io = new BufferedIo();
        var machine = new FixedMachine(io);

        machine.Shift(-1);
        machine.Add(7);
        machine.Shift(30_001);
        machine.Add(5);
        machine.Shift(-30_001);
        machine.Write();
        machine.Shift(1);
        machine.Write();

        Assert.Equal([(byte)7, (byte)5], io.Output);
    }

    [Fact]
    public void ReadAndWrite_UseCurrentCell()
    {
        using var io = new BufferedIo([65]);
        var machine = new FixedMachine(io);

        machine.Read();
        machine.Write();

        Assert.Equal([(byte)65], io.Output);
    }

    [Fact]
    public void Set_OverwritesCurrentCell()
    {
        using var io = new BufferedIo();
        var machine = new FixedMachine(io);

        machine.Add(10);
        machine.Set(7);
        machine.Write();

        Assert.Equal([(byte)7], io.Output);
    }

    [Fact]
    public void Mul_AddsProductToBufferAndPreservesSource()
    {
        using var io = new BufferedIo();
        var machine = new FixedMachine(io);
        machine.Add(3);
        machine.Shift(1);
        machine.Add(4);
        machine.Shift(-1);

        machine.Mul(2, 1);
        machine.Shift(1);
        machine.Write();
        machine.Shift(-1);
        machine.Write();

        Assert.Equal([(byte)10, (byte)3], io.Output);
    }

    [Fact]
    public void MulAndClear_AddsProductToBufferAndClearsSource()
    {
        using var io = new BufferedIo();
        var machine = new FixedMachine(io);
        machine.Add(3);
        machine.Shift(1);
        machine.Add(4);
        machine.Shift(-1);

        machine.MulAndClear(2, 1);
        machine.Shift(1);
        machine.Write();
        machine.Shift(-1);
        machine.Write();

        Assert.Equal([(byte)10, (byte)0], io.Output);
    }

    [Fact]
    public void IsZero_ReflectsCurrentCellValue()
    {
        using var io = new BufferedIo();
        var machine = new FixedMachine(io);

        Assert.True(machine.IsZero());
        machine.Add(1);
        Assert.False(machine.IsZero());
        machine.Add(-1);
        Assert.True(machine.IsZero());
    }
}
