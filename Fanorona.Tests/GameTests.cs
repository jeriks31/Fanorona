using Fanorona.Core;
using static Fanorona.Tests.TestBoards;

namespace Fanorona.Tests;

public class GameTests
{
    private static Turn FindTurn(Game game, string input)
    {
        var candidates = MoveGenerator.LenientTurns(game.Position);
        var result = Assert.IsType<ParseResult.Matched>(Notation.Parse(input, candidates));
        return result.Turn;
    }

    private static void ApplyByNotation(Game game, string input)
    {
        var turn = FindTurn(game, input);
        game.Apply(turn, MoveGenerator.IsLenientOnly(game.Position, turn));
    }

    [Fact]
    public void Undo_RestoresThePreviousPositionAndHash()
    {
        var game = new Game();
        ApplyByNotation(game, "e2-e3A");
        var afterWhite = game.Position;
        var hashesAfterWhite = game.HistoryHashes.ToList();
        ApplyByNotation(game, "f4-e5W-e4A");

        Assert.True(game.Undo());
        Assert.Equal(afterWhite, game.Position);
        Assert.Equal(hashesAfterWhite, game.HistoryHashes);
        Assert.True(game.Undo());
        Assert.Equal(Position.Initial, game.Position);
        Assert.False(game.Undo());
    }

    [Fact]
    public void SaveAndLoad_RoundTripsIncludingLenientMoves()
    {
        var game = new Game { EngineSide = Player.Black };
        ApplyByNotation(game, "e2-e3A");
        // d4-e4 is a pure paika while f4-e5W is available: legal only leniently (AC-style).
        var paika = FindTurn(game, "d4-e4");
        Assert.True(MoveGenerator.IsLenientOnly(game.Position, paika));
        game.Apply(paika, lenient: true);

        var text = game.Serialize();
        Assert.Contains("Engine: Black", text);
        Assert.Contains("1. W e2-e3A", text);
        Assert.Contains("1. B d4-e4!", text);

        var loaded = Game.Deserialize(text);
        Assert.Equal(Player.Black, loaded.EngineSide);
        Assert.Equal(game.Position, loaded.Position);
        Assert.Equal(game.Moves.Select(m => (Notation.Format(m.Turn), m.Lenient)),
            loaded.Moves.Select(m => (Notation.Format(m.Turn), m.Lenient)));
    }

    [Theory]
    [InlineData("1. W e2-e3A")]                       // missing Engine: line
    [InlineData("Engine: White\n1. B e2-e3A")]        // wrong side to move
    [InlineData("Engine: White\n1. W a1-a9")]         // unreadable move
    [InlineData("Engine: White\n1. W b2-b3")]         // not a possible move
    public void Deserialize_RejectsInvalidInput(string text)
    {
        Assert.Throws<FormatException>(() => Game.Deserialize(text));
    }

    [Fact]
    public void ThreefoldRepetition_IsADraw()
    {
        var start = Parse("""
            . . . . . . . . B
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            W . . . . . . . .
            """, Player.White);
        var game = new Game(start);
        foreach (var move in (string[])["a1-b1", "i5-h5", "b1-a1", "h5-i5",
                                        "a1-b1", "i5-h5", "b1-a1"])
        {
            Assert.Null(game.Outcome);
            ApplyByNotation(game, move);
        }
        Assert.Null(game.Outcome);
        ApplyByNotation(game, "h5-i5"); // the start position occurs for the third time
        Assert.Equal(GameOutcome.Draw, game.Outcome);
    }

    [Fact]
    public void CapturingAllPieces_WinsTheGame()
    {
        var start = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            W . B B . . . . .
            """, Player.White);
        var game = new Game(start);
        ApplyByNotation(game, "a1-b1A");
        Assert.Equal(GameOutcome.WhiteWins, game.Outcome);
    }
}
