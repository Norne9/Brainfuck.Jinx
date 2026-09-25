using Brainfuck.Jinx.Executor;

namespace Brainfuck.Jinx.Parser.Patterns;

/// <summary>
/// A single non-counter cell that one iteration of a loop modifies.
/// </summary>
/// <param name="Offset">
/// The cell's position relative to the loop's counter cell (the cell the
/// pointer rests on while the loop runs).
/// </param>
/// <param name="Value">The net amount added to that cell per iteration.</param>
public readonly record struct LoopDestination(int Offset, int Value);

/// <summary>
/// A precomputed summary of a loop body, shared by every <see cref="IPattern"/>
/// so that the body is inspected only once per loop.
/// </summary>
/// <remarks>
/// <para>
/// A loop qualifies as <see cref="IsArithmetic"/> when its body contains only
/// cell arithmetic (<see cref="OpCodeType.Add"/>) and pointer movement
/// (<see cref="OpCodeType.Shift"/>) and the pointer ends where it started.
/// Such a loop has a closed-form effect: it adds <see cref="CounterDelta"/> to
/// the counter cell and, for every entry in <see cref="Destinations"/>, adds a
/// fixed multiple of the counter to another cell. That is exactly the shape the
/// <c>SetZero</c>/<c>Mul</c>/<c>MulAndClear</c> op-codes execute, so the whole
/// loop can be replaced without iterating it.
/// </para>
/// <para>
/// Loops that do anything else (I/O, a nested loop, an already-flattened
/// multiplication) report <see cref="IsArithmetic"/> as <see langword="false"/>
/// and are summarised without allocating any heap memory.
/// </para>
/// </remarks>
public readonly struct LoopAnalysis
{
    /// <summary>
    /// Orders destinations by their offset. Held as a static field so the
    /// comparison is not re-created for every sort.
    /// </summary>
    private static readonly Comparison<LoopDestination> ByOffset =
        static (left, right) => left.Offset.CompareTo(right.Offset);

    private readonly LoopDestination[]? _destinations;

    private LoopAnalysis(bool isArithmetic, int counterDelta, LoopDestination[]? destinations)
    {
        IsArithmetic = isArithmetic;
        CounterDelta = counterDelta;
        _destinations = destinations;
    }

    /// <summary>
    /// Gets a value indicating whether the body is a flat arithmetic loop that
    /// can be summarised. When <see langword="false"/> the loop must keep its
    /// generic <see cref="OpCodeType.Loop"/> representation.
    /// </summary>
    public bool IsArithmetic { get; }

    /// <summary>
    /// Gets the net amount one iteration adds to the counter cell. Only
    /// meaningful when <see cref="IsArithmetic"/> is <see langword="true"/>.
    /// </summary>
    public int CounterDelta { get; }

    /// <summary>
    /// Gets the non-counter cells modified by one iteration, sorted by ascending
    /// <see cref="LoopDestination.Offset"/>. Empty unless <see cref="IsArithmetic"/>
    /// is <see langword="true"/>.
    /// </summary>
    public IReadOnlyList<LoopDestination> Destinations =>
        _destinations ?? Array.Empty<LoopDestination>();

    /// <summary>
    /// Summarises a loop <paramref name="body"/>.
    /// </summary>
    /// <param name="body">The non-empty op-code body of a loop.</param>
    /// <returns>
    /// The summary, or <see langword="default"/> (with
    /// <see cref="IsArithmetic"/> <see langword="false"/>) when the body is not
    /// a flat arithmetic loop.
    /// </returns>
    public static LoopAnalysis Analyze(IReadOnlyList<OpCode> body)
    {
        // First pass: validate without allocating. Failure is the common case
        // (loops containing I/O, nested loops, or flattened multiplications),
        // so it must be cheap. This pass also confirms the pointer returns to
        // the counter cell, which guarantees the per-cell offsets are stable
        // across iterations.
        var pointer = 0;
        foreach (var op in body)
        {
            switch (op.Type)
            {
                case OpCodeType.Shift:
                    pointer += op.Value;
                    break;
                case OpCodeType.Add:
                    break;
                default:
                    return default;
            }
        }

        if (pointer != 0)
        {
            return default;
        }

        // Second pass: fold each cell's increments. Only bodies that passed the
        // validation above reach this point.
        var deltas = new Dictionary<int, int>();
        pointer = 0;
        foreach (var op in body)
        {
            if (op.Type == OpCodeType.Shift)
            {
                pointer += op.Value;
            }
            else
            {
                deltas[pointer] = deltas.GetValueOrDefault(pointer) + op.Value;
            }
        }

        // Materialise the non-counter, non-zero cells. The buffer is sized for
        // the worst case and only copied when entries were skipped.
        var buffer = new LoopDestination[deltas.Count];
        var count = 0;
        foreach (var (offset, value) in deltas)
        {
            if (offset != 0 && value != 0)
            {
                buffer[count++] = new LoopDestination(offset, value);
            }
        }

        var destinations = count == buffer.Length ? buffer : buffer[..count];

        // Patterns rely on ascending offset order (the last destination becomes
        // the MulAndClear in MulPattern). Sort in place instead of using LINQ.
        if (destinations.Length > 1)
        {
            Array.Sort(destinations, ByOffset);
        }

        return new LoopAnalysis(true, deltas.GetValueOrDefault(0), destinations);
    }
}
