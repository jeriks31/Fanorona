using System.Collections.Immutable;
using System.Numerics;

namespace Fanorona.Core;

public enum CaptureKind
{
    None,
    Approach,
    Withdrawal,
}

/// <summary>
/// One step of a turn: a piece moves From -> To along a board line.
/// <paramref name="Captured"/> is the bitmask of enemy pieces removed by this step
/// (empty iff <paramref name="Kind"/> is None).
/// </summary>
public readonly record struct Segment(Cell From, Cell To, CaptureKind Kind, ulong Captured);

/// <summary>
/// A complete player turn: a single paika step, or a chain of one or more capturing steps.
/// Turns are produced by <see cref="MoveGenerator"/> and are legal for the position they
/// were generated from; <see cref="Position.Apply"/> relies on that.
/// </summary>
public sealed class Turn : IEquatable<Turn>
{
    public ImmutableArray<Segment> Segments { get; }

    public Turn(ImmutableArray<Segment> segments)
    {
        Segments = segments;
        foreach (var segment in segments)
            CapturedMask |= segment.Captured;
    }

    public bool IsCapture => Segments[0].Kind != CaptureKind.None;
    public ulong CapturedMask { get; }
    public int CapturedCount => BitOperations.PopCount(CapturedMask);
    public Cell Origin => Segments[0].From;
    public Cell Destination => Segments[^1].To;

    public bool Equals(Turn? other) =>
        other is not null && Segments.SequenceEqual(other.Segments);

    public override bool Equals(object? obj) => obj is Turn turn && Equals(turn);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var segment in Segments)
            hash.Add(segment);
        return hash.ToHashCode();
    }

    public override string ToString() => Notation.Format(this);
}
