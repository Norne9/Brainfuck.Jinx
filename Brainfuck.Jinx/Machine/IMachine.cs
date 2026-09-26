namespace Brainfuck.Jinx.Machine;

/// <summary>
/// The mutable state a Brainfuck program operates on: a fixed tape of byte
/// cells, a data pointer, and the input/output channel.
/// </summary>
/// <remarks>
/// <para>
/// This is the reference abstraction dispatched by
/// <see cref="Brainfuck.Jinx.Executor.InterpreterExecutor"/> and the
/// machine-based path of <see cref="Brainfuck.Jinx.Executor.JitExecutor"/>.
/// The fast inlined JIT path bypasses this interface entirely but implements the
/// same operations directly against a local byte array.
/// </para>
/// <para>
/// Cell arithmetic wraps modulo 256 and pointer movement wraps around the tape,
/// exactly as required by Brainfuck's usual fixed-tape interpretation.
/// </para>
/// </remarks>
public interface IMachine
{
    /// <summary>
    /// Adds <paramref name="value"/> to the current cell, wrapping modulo 256.
    /// </summary>
    /// <param name="value">The signed amount to add.</param>
    void Add(int value);

    /// <summary>
    /// Moves the data pointer by <paramref name="value"/> cells, wrapping around
    /// the tape.
    /// </summary>
    /// <param name="value">The signed number of cells to move.</param>
    void Shift(int value);

    /// <summary>Writes the current cell to the output channel.</summary>
    void Write();

    /// <summary>Reads one byte from the input channel into the current cell.</summary>
    void Read();

    /// <summary>Overwrites the current cell with <paramref name="value"/>, modulo 256.</summary>
    /// <param name="value">The value to store.</param>
    void Set(int value);

    /// <summary>
    /// Adds <c><paramref name="value"/> * currentCell</c> to the cell at
    /// <c>position + <paramref name="buffer"/></c>, leaving the current cell
    /// unchanged.
    /// </summary>
    /// <param name="value">The multiplier.</param>
    /// <param name="buffer">The destination cell's offset from the current position.</param>
    void Mul(int value, int buffer);

    /// <summary>
    /// Like <see cref="Mul"/>, then clears the current cell to zero.
    /// </summary>
    /// <param name="value">The multiplier.</param>
    /// <param name="buffer">The destination cell's offset from the current position.</param>
    void MulAndClear(int value, int buffer);

    /// <summary>
    /// Replaces the cell at <c>position + <paramref name="buffer"/></c> with
    /// <c><paramref name="value"/> * thatCell * currentCell</c>.
    /// </summary>
    /// <param name="value">The multiplier.</param>
    /// <param name="buffer">The destination cell's offset from the current position.</param>
    void MulAndMul(int value, int buffer);

    /// <summary>
    /// Shifts the pointer by <paramref name="direction"/> repeatedly until the
    /// current cell is zero.
    /// </summary>
    /// <param name="direction">The stride of each shift.</param>
    void PointerScan(int direction);

    /// <summary>
    /// Gets a value indicating whether the current cell is zero.
    /// </summary>
    /// <returns><see langword="true"/> when the current cell is zero.</returns>
    bool IsZero();
}
