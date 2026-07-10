using System.Numerics;

namespace Fanorona.Core;

/// <summary>
/// Static evaluation from the side to move's perspective. Material dominates completely in
/// Fanorona; a small bonus for occupying strong (8-way) points breaks material ties.
/// </summary>
public static class Evaluation
{
    public const int Win = 1_000_000;

    private static readonly ulong StrongMask = BuildStrongMask();

    public static int Evaluate(in Position position)
    {
        var own = position.Pieces(position.ToMove);
        var enemy = position.Pieces(position.ToMove.Opponent());
        return 100 * (BitOperations.PopCount(own) - BitOperations.PopCount(enemy))
             + 2 * (BitOperations.PopCount(own & StrongMask) - BitOperations.PopCount(enemy & StrongMask));
    }

    private static ulong BuildStrongMask()
    {
        ulong mask = 0;
        for (var cell = 0; cell < Board.Cells; cell++)
        {
            if (Board.IsStrong(cell))
                mask |= 1UL << cell;
        }
        return mask;
    }
}
