using Fanorona.Core;
using static Fanorona.Tests.TestBoards;

namespace Fanorona.Tests;

public class InitialPositionTests
{
    [Fact]
    public void HasStandardSetup()
    {
        var p = Position.Initial;
        Assert.Equal(22, p.PieceCount(Player.White));
        Assert.Equal(22, p.PieceCount(Player.Black));
        Assert.True(p.IsEmpty(C("e3").Index));
        Assert.Equal(Player.White, p.ToMove);
    }

    [Fact]
    public void IsSymmetricUnder180DegreeRotationWithColorSwap()
    {
        var p = Position.Initial;
        for (var cell = 0; cell < Board.Cells; cell++)
        {
            var rotated = Board.Cells - 1 - cell;
            Assert.Equal((p.White & 1UL << cell) != 0, (p.Black & 1UL << rotated) != 0);
        }
    }

    [Fact]
    public void OpeningHasExactlyFiveTurns_AllSingleSegmentCaptures()
    {
        var turns = MoveGenerator.LegalTurns(Position.Initial);
        Assert.Equal(
            new[] { "d2-e3A", "d3-e3A", "d3-e3W", "e2-e3A", "f2-e3A" },
            Notations(turns));
        Assert.All(turns, t => Assert.Single(t.Segments));
        Assert.All(turns, t => Assert.True(t.IsCapture));
    }

    [Fact]
    public void OpeningCapturesRemoveTheExpectedPieces()
    {
        var turns = MoveGenerator.LegalTurns(Position.Initial);
        var byNotation = turns.ToDictionary(Notation.Format);
        Assert.Equal(Mask("e4", "e5"), byNotation["e2-e3A"].CapturedMask);
        Assert.Equal(Mask("f4", "g5"), byNotation["d2-e3A"].CapturedMask);
        Assert.Equal(Mask("d4", "c5"), byNotation["f2-e3A"].CapturedMask);
        Assert.Equal(Mask("f3"), byNotation["d3-e3A"].CapturedMask); // line blocked by white g3
        Assert.Equal(Mask("c3"), byNotation["d3-e3W"].CapturedMask); // line blocked by white b3
    }
}
