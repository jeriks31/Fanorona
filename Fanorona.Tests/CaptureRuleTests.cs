using Fanorona.Core;
using static Fanorona.Tests.TestBoards;

namespace Fanorona.Tests;

public class CaptureRuleTests
{
    [Fact]
    public void Approach_CapturesFullContiguousLine_StoppingAtGap()
    {
        var p = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            W . B B B . B . .
            """, Player.White);
        var turn = Assert.Single(MoveGenerator.LegalTurns(p));
        Assert.Equal("a1-b1A", Notation.Format(turn));
        Assert.Equal(Mask("c1", "d1", "e1"), turn.CapturedMask); // g1 survives beyond the gap
        var after = p.Apply(turn);
        Assert.Equal(Mask("b1"), after.White);
        Assert.Equal(Mask("g1"), after.Black);
    }

    [Fact]
    public void Approach_LineStopsAtOwnPiece()
    {
        var p = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            W . B B W B . . .
            """, Player.White);
        var turn = Assert.Single(MoveGenerator.LegalTurns(p));
        Assert.Equal("a1-b1A", Notation.Format(turn));
        Assert.Equal(Mask("c1", "d1"), turn.CapturedMask);
    }

    [Fact]
    public void Withdrawal_CapturesLineBehindTheOrigin()
    {
        var p = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            B B W . . . . . .
            """, Player.White);
        var turn = Assert.Single(MoveGenerator.LegalTurns(p));
        Assert.Equal("c1-d1W", Notation.Format(turn));
        Assert.Equal(Mask("a1", "b1"), turn.CapturedMask);
    }

    [Fact]
    public void SameMovement_WithBothCaptures_YieldsTwoDistinctTurns()
    {
        var p = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            B B . W B B . . .
            """, Player.White);
        var turns = MoveGenerator.LegalTurns(p);
        Assert.Equal(new[] { "d1-c1A", "d1-c1W" }, Notations(turns));
        var byNotation = turns.ToDictionary(Notation.Format);
        Assert.Equal(Mask("a1", "b1"), byNotation["d1-c1A"].CapturedMask);
        Assert.Equal(Mask("e1", "f1"), byNotation["d1-c1W"].CapturedMask);
        // One segment never captures both lines at once.
        Assert.All(turns, t => Assert.Equal(2, t.CapturedCount));
    }

    [Fact]
    public void PaikaMoves_OnlyExistWhenNoCaptureIsAvailable()
    {
        var withCaptures = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            B B . W B B . . .
            """, Player.White);
        Assert.All(MoveGenerator.LegalTurns(withCaptures), t => Assert.True(t.IsCapture));
        // Lenient list adds the paika moves (d1-c1 and d1-d2) for AC opponent input.
        var lenient = MoveGenerator.LenientTurns(withCaptures);
        Assert.Equal(2, lenient.Count(t => !t.IsCapture));

        var noCaptures = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            W . . . . . . . B
            """, Player.White);
        var turns = MoveGenerator.LegalTurns(noCaptures);
        Assert.Equal(3, turns.Count); // a1-a2, a1-b1, a1-b2
        Assert.All(turns, t => Assert.False(t.IsCapture));
    }
}
