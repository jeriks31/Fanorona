using Fanorona.Core;
using static Fanorona.Tests.TestBoards;

namespace Fanorona.Tests;

public class EngineTests
{
    [Fact]
    public void FindsAMateInOne()
    {
        var p = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            W . B B B . . . .
            """, Player.White);
        var result = new Engine().FindBestTurn(p, TimeSpan.FromSeconds(1), [],
            TestContext.Current.CancellationToken);
        Assert.Equal("a1-b1A", Notation.Format(result.BestTurn));
        Assert.True(result.Score > 900_000);
        Assert.Equal(0UL, p.Apply(result.BestTurn).Black);
    }

    [Fact]
    public void SearchIsDeterministicAtAFixedDepth()
    {
        var first = new Engine().FindBestTurn(Position.Initial, TimeSpan.FromSeconds(30),
            [Zobrist.Compute(Position.Initial)], TestContext.Current.CancellationToken, maxDepth: 5);
        var second = new Engine().FindBestTurn(Position.Initial, TimeSpan.FromSeconds(30),
            [Zobrist.Compute(Position.Initial)], TestContext.Current.CancellationToken, maxDepth: 5);
        Assert.Equal(Notation.Format(first.BestTurn), Notation.Format(second.BestTurn));
        Assert.Equal(first.Score, second.Score);
        Assert.Equal(first.Nodes, second.Nodes);
    }

    [Fact]
    public void BestTurn_IsAlwaysStrictlyLegal()
    {
        var engine = new Engine();
        var random = new Random(99);
        var position = Position.Initial;
        for (var ply = 0; ply < 30; ply++)
        {
            var turns = MoveGenerator.LegalTurns(position);
            if (turns.Count == 0)
                break;
            var result = engine.FindBestTurn(position, TimeSpan.FromMilliseconds(30),
                [Zobrist.Compute(position)], TestContext.Current.CancellationToken);
            Assert.Contains(result.BestTurn, turns);
            position = position.Apply(turns[random.Next(turns.Count)]);
        }
    }

    [Fact]
    public void Engine_BeatsARandomMover()
    {
        var engine = new Engine();
        var random = new Random(7);
        var game = new Game { EngineSide = Player.White };
        for (var ply = 0; ply < 300 && game.Outcome == null; ply++)
        {
            var turns = MoveGenerator.LegalTurns(game.Position);
            var turn = game.Position.ToMove == Player.White
                ? engine.FindBestTurn(game.Position, TimeSpan.FromMilliseconds(100),
                    game.HistoryHashes, TestContext.Current.CancellationToken).BestTurn
                : turns[random.Next(turns.Count)];
            game.Apply(turn);
        }
        Assert.Equal(GameOutcome.WhiteWins, game.Outcome);
    }
}
