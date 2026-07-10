using System.Numerics;
using Fanorona.Core;

namespace Fanorona.Web;

public static class MoveDescriber
{
    /// <summary>One-line form used in move lists.</summary>
    public static string Summary(Turn turn) => turn.IsCapture
        ? $"{Notation.Format(turn)}  (captures {CellList(turn.CapturedMask)})"
        : $"{Notation.Format(turn)}  (no capture)";

    /// <summary>Step-by-step instructions; item i matches the board overlay's badge i+1.</summary>
    public static IReadOnlyList<string> Steps(Turn turn)
    {
        if (!turn.IsCapture)
            return [$"Move {turn.Origin} to {turn.Destination} — no capture (paika)."];
        var steps = new List<string>();
        for (var i = 0; i < turn.Segments.Length; i++)
        {
            var segment = turn.Segments[i];
            var verb = i == 0 ? "Move" : "Then";
            var kind = segment.Kind == CaptureKind.Approach ? "approach" : "withdrawal";
            steps.Add($"{verb} {segment.From} to {segment.To} — {kind}, removing {CellList(segment.Captured)}.");
        }
        return steps;
    }

    public static string CellList(ulong mask)
    {
        var cells = new List<string>();
        for (var bits = mask; bits != 0; bits &= bits - 1)
            cells.Add(new Cell(BitOperations.TrailingZeroCount(bits)).ToString());
        return string.Join(", ", cells);
    }
}
