using Fanorona.Core;

namespace Fanorona.Web;

/// <summary>SVG coordinates for the board. Rank 1 is at the bottom unless flipped.</summary>
public static class BoardGeometry
{
    public const int Spacing = 100;
    public const int Margin = 60;
    public const int Width = Margin * 2 + (Board.Files - 1) * Spacing;
    public const int Height = Margin * 2 + (Board.Ranks - 1) * Spacing;

    public static (int X, int Y) Point(Cell cell, bool flipped)
    {
        var column = flipped ? Board.Files - 1 - cell.File : cell.File;
        var row = flipped ? cell.Rank : Board.Ranks - 1 - cell.Rank;
        return (Margin + column * Spacing, Margin + row * Spacing);
    }

    /// <summary>
    /// Every board line exactly once. Taking only E, N, NE and NW from each cell covers each
    /// undirected edge from one side; diagonals exist only between strong points, so
    /// <see cref="Board.Neighbor"/> already filters them.
    /// </summary>
    public static IEnumerable<(Cell A, Cell B)> Lines()
    {
        int[] dirs = [0, 2, 4, 5]; // E, N, NE, NW
        for (var cell = 0; cell < Board.Cells; cell++)
        {
            foreach (var dir in dirs)
            {
                var to = Board.Neighbor(cell, dir);
                if (to >= 0)
                    yield return (new Cell(cell), new Cell(to));
            }
        }
    }
}
