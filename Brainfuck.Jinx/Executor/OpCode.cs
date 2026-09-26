namespace Brainfuck.Jinx.Executor;

/// <summary>
/// The kind of single operation stored in an <see cref="OpCode"/>.
/// </summary>
/// <remarks>
/// <para>
/// The lower half of the enum (<see cref="Add"/> .. <see cref="Read"/>) maps
/// one-to-one onto Brainfuck's eight instructions. The upper half
/// (<see cref="Set"/> .. <see cref="PointerScan"/>) contains fused, purpose-built
/// operations that the <see cref="Brainfuck.Jinx.Parser.OptimizingParser"/>
/// produces when it recognises a well-known idiom; each one does in a single
/// dispatch what a whole loop used to do.
/// </para>
/// <para>
/// All cell values are bytes and therefore wrap modulo 256. All pointer movement
/// wraps around the fixed 30,000-cell tape.
/// </para>
/// </remarks>
public enum OpCodeType
{
    /// <summary>
    /// Adds the signed constant <see cref="OpCode.Value"/> to the current cell,
    /// wrapping modulo 256. Corresponds to a run of <c>+</c>/<c>-</c>.
    /// </summary>
    Add,

    /// <summary>
    /// Moves the data pointer by the signed constant <see cref="OpCode.Value"/>,
    /// wrapping around the 30,000-cell tape. Corresponds to a run of
    /// <c>&gt;</c>/<c>&lt;</c>.
    /// </summary>
    Shift,

    /// <summary>
    /// Writes the current cell to the output as a single byte. Corresponds to
    /// <c>.</c>.
    /// </summary>
    Write,

    /// <summary>
    /// Reads a single byte from the input into the current cell. Corresponds to
    /// <c>,</c>.
    /// </summary>
    Read,

    /// <summary>
    /// Runs the op-codes in <see cref="OpCode.OpCodes"/> repeatedly while the
    /// current cell is non-zero. Corresponds to a <c>[ ... ]</c> block.
    /// </summary>
    Loop,

    /// <summary>
    /// Overwrites the current cell with the constant <see cref="OpCode.Value"/>
    /// (modulo 256). Produced from a clearing loop such as <c>[-]</c>.
    /// </summary>
    Set,

    /// <summary>
    /// Adds <c>Value * memory[position]</c> to the cell at the relative
    /// destination <see cref="OpCode.Buffer"/>, leaves the source cell intact,
    /// then shifts the pointer by <see cref="OpCode.Offset"/>. This is the
    /// non-final step of a multi-destination multiplication loop.
    /// </summary>
    Mul,

    /// <summary>
    /// Like <see cref="Mul"/>, but clears the source (counter) cell to zero
    /// afterwards. This is the final step of a multiplication loop, e.g.
    /// <c>[->+&lt;]</c>.
    /// </summary>
    MulAndClear,

    /// <summary>
    /// Replaces the cell at the relative destination
    /// <see cref="OpCode.Buffer"/> with
    /// <c>memory[destination] * Value * memory[position]</c>, then shifts the
    /// pointer by <see cref="OpCode.Offset"/>. Used by the nested
    /// <c>[->[->+&lt;]&lt;]</c> idiom.
    /// </summary>
    MulAndMul,

    /// <summary>
    /// Repeatedly shifts the pointer by the signed constant
    /// <see cref="OpCode.Value"/> while the current cell is non-zero.
    /// Corresponds to the <c>[&gt;]</c>/<c>[&lt;]</c> tape-walking idiom.
    /// </summary>
    PointerScan,

    /// <summary>
    /// Stops the program immediately when the current cell is non-zero; does
    /// nothing when it is zero. Produced from an empty loop <c>[]</c>, which
    /// would otherwise hang the executor.
    /// </summary>
    Halt
}

/// <summary>
/// A single, executable Brainfuck operation, optionally holding a nested
/// <see cref="OpCodes"/> body for <see cref="OpCodeType.Loop"/>.
/// </summary>
/// <param name="Type">The operation to perform; selects which fields are meaningful.</param>
/// <param name="Value">
/// The primary operand. Its meaning depends on <see cref="Type"/>: the signed
/// amount for <see cref="OpCodeType.Add"/> / <see cref="OpCodeType.Shift"/> /
/// <see cref="OpCodeType.PointerScan"/>, the constant for
/// <see cref="OpCodeType.Set"/>, or the multiplier for the <c>Mul</c> family.
/// </param>
/// <param name="OpCodes">
/// The body of a <see cref="OpCodeType.Loop"/>; <see langword="null"/> for every
/// other operation.
/// </param>
/// <param name="Offset">
/// A pointer shift that the <c>Mul</c> family performs after its arithmetic.
/// Zero for every other operation.
/// </param>
/// <param name="Buffer">
/// The destination cell, relative to the current position, for the <c>Mul</c>
/// family. Zero for every other operation.
/// </param>
public readonly record struct OpCode(
    OpCodeType Type,
    int Value = 0,
    List<OpCode>? OpCodes = null,
    int Offset = 0,
    int Buffer = 0)
{
    /// <summary>
    /// Returns a compact, human-readable rendering of the operation, including
    /// the full body of a loop. Used by the shell's <c>--opcodes</c> output and
    /// by test failure messages.
    /// </summary>
    /// <returns>A debugger-friendly description of this op-code.</returns>
    public override string ToString() =>
        this.Type switch
        {
            OpCodeType.Add => $"Add({Value})",
            OpCodeType.Shift => $"Shift({Value})",
            OpCodeType.Write => "Write",
            OpCodeType.Read => "Read",
            OpCodeType.Loop => "Loop[" + string.Join(", ", OpCodes ?? []) + "]",
            OpCodeType.Set => $"Set({Value})",
            OpCodeType.Mul => $"Mul(val={Value} buf={Buffer} off={Offset})",
            OpCodeType.MulAndClear => $"MulAndClear(val={Value} buf={Buffer} off={Offset})",
            OpCodeType.MulAndMul => $"MulAndMul(val={Value} buf={Buffer} off={Offset})",
            OpCodeType.PointerScan => $"PointerScan({Value})",
            OpCodeType.Halt => "Halt",
            _ => throw new ArgumentOutOfRangeException()
        };
}
