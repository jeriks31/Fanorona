using System.Text;
using Fanorona.Core;

namespace Fanorona.ConsoleApp;

public static class BoardRenderer
{
    /// <summary>
    /// Renders the board as Spectre markup. The last turn's path, destination, and captured
    /// cells are highlighted. With <paramref name="flipped"/> the board is rotated 180°
    /// (coordinates unchanged) to match the AC camera angle.
    /// </summary>
    public static string Render(Position position, Turn? lastTurn, bool flipped)
    {
        var capturedMask = lastTurn?.CapturedMask ?? 0;
        var destinationMask = lastTurn?.Destination.Bit ?? 0;
        var pathMask = 0UL;
        if (lastTurn != null)
        {
            pathMask = lastTurn.Origin.Bit;
            foreach (var segment in lastTurn.Segments)
                pathMask |= segment.To.Bit;
            pathMask &= ~destinationMask;
        }

        int[] rankOrder = flipped ? [0, 1, 2, 3, 4] : [4, 3, 2, 1, 0];
        int[] fileOrder = flipped ? [8, 7, 6, 5, 4, 3, 2, 1, 0] : [0, 1, 2, 3, 4, 5, 6, 7, 8];

        var sb = new StringBuilder();
        for (var row = 0; row < Board.Ranks; row++)
        {
            var rank = rankOrder[row];
            sb.Append($"[grey] {rank + 1}[/]  ");
            for (var col = 0; col < Board.Files; col++)
            {
                sb.Append(Glyph(position, Cell.At(fileOrder[col], rank),
                    capturedMask, destinationMask, pathMask));
                if (col < Board.Files - 1)
                    sb.Append("[grey]─[/]");
            }
            sb.Append('\n');
            if (row < Board.Ranks - 1)
            {
                sb.Append("    ");
                var lowerRank = rankOrder[row + 1];
                for (var col = 0; col < Board.Files; col++)
                {
                    sb.Append("[grey]│[/]");
                    if (col < Board.Files - 1)
                    {
                        var topLeft = Cell.At(fileOrder[col], rank);
                        var bottomRight = Cell.At(fileOrder[col + 1], lowerRank);
                        sb.Append(AreDiagonalNeighbors(topLeft, bottomRight) ? "[grey]╲[/]" : "[grey]╱[/]");
                    }
                }
                sb.Append('\n');
            }
        }
        sb.Append("[grey]    ");
        foreach (var file in fileOrder)
            sb.Append((char)('a' + file)).Append(' ');
        sb.Append("[/]\n");
        return sb.ToString();
    }

    private static string Glyph(Position position, Cell cell,
        ulong capturedMask, ulong destinationMask, ulong pathMask)
    {
        var isWhite = (position.White & cell.Bit) != 0;
        var isBlack = (position.Black & cell.Bit) != 0;
        if ((destinationMask & cell.Bit) != 0 && (isWhite || isBlack))
            return isWhite ? "[bold white on green]●[/]" : "[bold orange1 on green]●[/]";
        if (isWhite)
            return "[bold white]●[/]";
        if (isBlack)
            return "[bold orange1]●[/]";
        if ((capturedMask & cell.Bit) != 0)
            return "[bold red]x[/]";
        if ((pathMask & cell.Bit) != 0)
            return "[bold green]·[/]";
        return "[grey]·[/]";
    }

    // Each lattice square has exactly one diagonal; it runs through the square's strong corners.
    private static bool AreDiagonalNeighbors(Cell a, Cell b) =>
        Board.IsStrong(a.Index) && Math.Abs(a.File - b.File) == 1 && Math.Abs(a.Rank - b.Rank) == 1;
}
