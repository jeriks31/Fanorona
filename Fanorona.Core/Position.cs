using System.Numerics;

namespace Fanorona.Core;

/// <summary>Immutable board state: one bitboard per color (bits 0..44) plus the side to move.</summary>
public readonly record struct Position(ulong White, ulong Black, Player ToMove)
{
    public ulong Occupied => White | Black;

    public ulong Pieces(Player player) => player == Player.White ? White : Black;

    public bool IsEmpty(int cell) => (Occupied & (1UL << cell)) == 0;

    public int PieceCount(Player player) => BitOperations.PopCount(Pieces(player));

    public static Position Initial { get; } = CreateInitial();

    private static Position CreateInitial()
    {
        ulong white = 0, black = 0;
        for (var file = 0; file < Board.Files; file++)
        {
            white |= Cell.At(file, 0).Bit | Cell.At(file, 1).Bit;
            black |= Cell.At(file, 3).Bit | Cell.At(file, 4).Bit;
        }
        // Middle rank: a3=B b3=W c3=B d3=W e3=empty f3=B g3=W h3=B i3=W
        // (180-degree rotation with color swap maps the position onto itself).
        foreach (var file in (int[])[1, 3, 6, 8])
            white |= Cell.At(file, 2).Bit;
        foreach (var file in (int[])[0, 2, 5, 7])
            black |= Cell.At(file, 2).Bit;
        return new Position(white, black, Player.White);
    }

    /// <summary>
    /// Plays a full turn and returns the resulting position. The turn must be legal for this
    /// position (it comes from <see cref="MoveGenerator"/> run on this exact position).
    /// </summary>
    public Position Apply(Turn turn)
    {
        var own = Pieces(ToMove);
        var enemy = Pieces(ToMove.Opponent());
        foreach (var segment in turn.Segments)
        {
            own = own & ~segment.From.Bit | segment.To.Bit;
            enemy &= ~segment.Captured;
        }
        return ToMove == Player.White
            ? new Position(own, enemy, Player.Black)
            : new Position(enemy, own, Player.White);
    }
}
