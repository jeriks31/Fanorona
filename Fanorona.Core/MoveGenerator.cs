using System.Collections.Immutable;
using System.Numerics;

namespace Fanorona.Core;

public static class MoveGenerator
{
    /// <summary>
    /// All turns legal under standard Fanorona rules: every capturing turn (each legal
    /// stopping point of a chain is its own turn), or every paika move if no capture exists.
    /// </summary>
    public static List<Turn> LegalTurns(in Position position)
    {
        var captures = CaptureTurns(position);
        return captures.Count > 0 ? captures : PaikaTurns(position);
    }

    /// <summary>
    /// Turns acceptable from the Assassin's Creed opponent, whose AI ignores the
    /// mandatory-capture rule: the strict legal turns plus all paika moves.
    /// </summary>
    public static List<Turn> LenientTurns(in Position position)
    {
        var captures = CaptureTurns(position);
        if (captures.Count == 0)
            return PaikaTurns(position);
        captures.AddRange(PaikaTurns(position));
        return captures;
    }

    /// <summary>True if the matched turn would be illegal under strict rules (paika while a capture exists).</summary>
    public static bool IsLenientOnly(in Position position, Turn turn) =>
        !turn.IsCapture && CaptureTurns(position).Count > 0;

    private static List<Turn> PaikaTurns(in Position position)
    {
        var result = new List<Turn>();
        var occupied = position.Occupied;
        for (var pieces = position.Pieces(position.ToMove); pieces != 0; pieces &= pieces - 1)
        {
            var from = BitOperations.TrailingZeroCount(pieces);
            for (var dir = 0; dir < Board.DirectionCount; dir++)
            {
                var to = Board.Neighbor(from, dir);
                if (to >= 0 && (occupied & 1UL << to) == 0)
                    result.Add(new Turn([new Segment(new Cell(from), new Cell(to), CaptureKind.None, 0)]));
            }
        }
        return result;
    }

    private static List<Turn> CaptureTurns(in Position position)
    {
        var result = new List<Turn>();
        var occupied = position.Occupied;
        var segments = new List<Segment>();
        for (var pieces = position.Pieces(position.ToMove); pieces != 0; pieces &= pieces - 1)
        {
            var from = BitOperations.TrailingZeroCount(pieces);
            for (var dir = 0; dir < Board.DirectionCount; dir++)
            {
                var to = Board.Neighbor(from, dir);
                if (to < 0 || (occupied & 1UL << to) != 0)
                    continue;
                foreach (var kind in (CaptureKind[])[CaptureKind.Approach, CaptureKind.Withdrawal])
                {
                    var captured = CapturedLine(position, from, to, dir, kind);
                    if (captured == 0)
                        continue;
                    var segment = new Segment(new Cell(from), new Cell(to), kind, captured);
                    segments.Add(segment);
                    ExtendChain(ApplySegment(position, segment), segments,
                        1UL << from | 1UL << to, dir, result);
                    segments.RemoveAt(segments.Count - 1);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Emits the chain built so far as a complete turn (stopping is always allowed), then
    /// recurses into every legal capturing continuation. A continuation must capture, may not
    /// land on any cell the piece has occupied this turn, and may not repeat the previous
    /// segment's direction.
    /// </summary>
    private static void ExtendChain(Position position, List<Segment> segments, ulong visited,
        int lastDir, List<Turn> result)
    {
        result.Add(new Turn([.. segments]));
        var current = segments[^1].To.Index;
        var occupied = position.Occupied;
        for (var dir = 0; dir < Board.DirectionCount; dir++)
        {
            if (dir == lastDir)
                continue;
            var to = Board.Neighbor(current, dir);
            if (to < 0 || (occupied & 1UL << to) != 0 || (visited & 1UL << to) != 0)
                continue;
            foreach (var kind in (CaptureKind[])[CaptureKind.Approach, CaptureKind.Withdrawal])
            {
                var captured = CapturedLine(position, current, to, dir, kind);
                if (captured == 0)
                    continue;
                var segment = new Segment(new Cell(current), new Cell(to), kind, captured);
                segments.Add(segment);
                ExtendChain(ApplySegment(position, segment), segments, visited | 1UL << to, dir, result);
                segments.RemoveAt(segments.Count - 1);
            }
        }
    }

    /// <summary>
    /// The contiguous line of enemy pieces captured by moving from -> to (direction dir):
    /// for an approach the line starts beyond the destination and extends along dir; for a
    /// withdrawal it starts behind the origin and extends along the opposite direction.
    /// </summary>
    private static ulong CapturedLine(in Position position, int from, int to, int dir, CaptureKind kind)
    {
        var enemy = position.Pieces(position.ToMove.Opponent());
        var lineDir = kind == CaptureKind.Approach ? dir : Board.Opposite(dir);
        var cell = kind == CaptureKind.Approach ? Board.Neighbor(to, lineDir) : Board.Neighbor(from, lineDir);
        ulong captured = 0;
        while (cell >= 0 && (enemy & 1UL << cell) != 0)
        {
            captured |= 1UL << cell;
            cell = Board.Neighbor(cell, lineDir);
        }
        return captured;
    }

    /// <summary>Applies one segment without flipping the side to move (used mid-chain).</summary>
    private static Position ApplySegment(in Position position, Segment segment)
    {
        var own = position.Pieces(position.ToMove) & ~segment.From.Bit | segment.To.Bit;
        var enemy = position.Pieces(position.ToMove.Opponent()) & ~segment.Captured;
        return position.ToMove == Player.White
            ? position with { White = own, Black = enemy }
            : position with { White = enemy, Black = own };
    }
}
