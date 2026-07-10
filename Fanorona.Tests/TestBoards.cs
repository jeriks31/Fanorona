using Fanorona.Core;

namespace Fanorona.Tests;

public static class TestBoards
{
    /// <summary>
    /// Builds a position from a 5-row diagram (rank 5 first, files a..i left to right),
    /// with W/B for pieces and '.' for empty; whitespace between cells is ignored.
    /// </summary>
    public static Position Parse(string diagram, Player toMove)
    {
        var rows = diagram.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(Board.Ranks, rows.Length);
        ulong white = 0, black = 0;
        for (var row = 0; row < Board.Ranks; row++)
        {
            var cells = rows[row].Replace(" ", "");
            Assert.Equal(Board.Files, cells.Length);
            var rank = Board.Ranks - 1 - row;
            for (var file = 0; file < Board.Files; file++)
            {
                var bit = Cell.At(file, rank).Bit;
                switch (cells[file])
                {
                    case 'W': white |= bit; break;
                    case 'B': black |= bit; break;
                    case '.': break;
                    default: Assert.Fail($"unexpected cell character '{cells[file]}'"); break;
                }
            }
        }
        return new Position(white, black, toMove);
    }

    public static Cell C(string name)
    {
        Assert.True(Cell.TryParse(name, out var cell));
        return cell;
    }

    public static ulong Mask(params string[] cells) =>
        cells.Aggregate(0UL, (mask, name) => mask | C(name).Bit);

    /// <summary>All cells the moving piece occupies during a turn: origin plus each destination.</summary>
    public static List<Cell> PathCells(Turn turn)
    {
        var path = new List<Cell> { turn.Origin };
        path.AddRange(turn.Segments.Select(s => s.To));
        return path;
    }

    /// <summary>Unit direction (df, dr) of a segment.</summary>
    public static (int Df, int Dr) Direction(Segment segment) =>
        (Math.Sign(segment.To.File - segment.From.File), Math.Sign(segment.To.Rank - segment.From.Rank));

    public static List<string> Notations(IEnumerable<Turn> turns) =>
        turns.Select(Notation.Format).OrderBy(n => n, StringComparer.Ordinal).ToList();
}
