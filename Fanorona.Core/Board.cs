namespace Fanorona.Core;

/// <summary>
/// Static geometry of the 9x5 Fanoron-Tsivy board. Files a..i (0..8), ranks 1..5 (0..4).
/// A point is "strong" (has diagonal lines) iff file + rank is even.
/// </summary>
public static class Board
{
    public const int Files = 9;
    public const int Ranks = 5;
    public const int Cells = Files * Ranks;
    public const int DirectionCount = 8;

    /// <summary>Fixed direction order: E, W, N, S, NE, NW, SE, SW.</summary>
    public static readonly (int Df, int Dr)[] Directions =
    [
        (1, 0), (-1, 0), (0, 1), (0, -1),
        (1, 1), (-1, 1), (1, -1), (-1, -1),
    ];

    private static readonly int[] OppositeDir = [1, 0, 3, 2, 7, 6, 5, 4];

    // Neighbor[cell * 8 + dir] = adjacent cell index, or -1 (off-board, or diagonal from a weak point).
    private static readonly int[] NeighborTable = BuildNeighborTable();

    public static int Opposite(int dir) => OppositeDir[dir];

    public static bool IsStrong(int cell) => (cell % Files + cell / Files) % 2 == 0;

    /// <summary>Adjacent cell along a board line, or -1 if no line exists in that direction.</summary>
    public static int Neighbor(int cell, int dir) => NeighborTable[cell * DirectionCount + dir];

    private static int[] BuildNeighborTable()
    {
        var table = new int[Cells * DirectionCount];
        for (var cell = 0; cell < Cells; cell++)
        {
            int file = cell % Files, rank = cell / Files;
            for (var dir = 0; dir < DirectionCount; dir++)
            {
                var (df, dr) = Directions[dir];
                int nf = file + df, nr = rank + dr;
                var offBoard = nf is < 0 or >= Files || nr is < 0 or >= Ranks;
                var noDiagonalLine = dir >= 4 && !IsStrong(cell);
                table[cell * DirectionCount + dir] = offBoard || noDiagonalLine ? -1 : nr * Files + nf;
            }
        }
        return table;
    }
}
