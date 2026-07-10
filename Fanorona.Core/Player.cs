namespace Fanorona.Core;

public enum Player
{
    White,
    Black,
}

public static class PlayerExtensions
{
    public static Player Opponent(this Player player) =>
        player == Player.White ? Player.Black : Player.White;
}
