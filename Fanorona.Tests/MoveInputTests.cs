using Fanorona.Core;
using Fanorona.Web;
using static Fanorona.Tests.TestBoards;

namespace Fanorona.Tests;

public class MoveInputTests
{
    private static MoveInput InputFor(Position position)
    {
        var input = new MoveInput();
        input.Reset(position);
        return input;
    }

    [Fact]
    public void Paika_SingleClickPairCompletesTheMove()
    {
        var p = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            W . . . . . . . .
            """, Player.White);
        var input = InputFor(p);
        input.Click(C("a1"));
        Assert.Equal(C("a1"), input.Origin);
        input.Click(C("b1"));
        Assert.Equal("a1-b1", Notation.Format(input.Completed!));
    }

    [Fact]
    public void ClickingTheSelectedOriginDeselects_AnotherOriginSwitches()
    {
        var input = InputFor(Position.Initial);
        input.Click(C("e2"));
        Assert.Equal(C("e2"), input.Origin);
        input.Click(C("d3"));
        Assert.Equal(C("d3"), input.Origin);
        input.Click(C("d3"));
        Assert.Null(input.Origin);
    }

    [Fact]
    public void ForcedSingleSegmentCapture_AutoFinishes()
    {
        // a1-b1A captures c1 only; nothing can extend the chain, so no stop confirmation.
        var p = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            W . B . . . . . .
            """, Player.White);
        var input = InputFor(p);
        input.Click(C("a1"));
        input.Click(C("b1"));
        Assert.Equal("a1-b1A", Notation.Format(input.Completed!));
        Assert.Equal(Mask("c1"), input.Completed!.CapturedMask);
    }

    [Fact]
    public void ChainWithStoppingPoint_RequiresExplicitStopOrContinuation()
    {
        // a1-b1A may stop or continue b1-b2A (same as Chain_EmitsEveryLegalStoppingPoint).
        var p = Parse("""
            . . . . . . . . .
            . B . . . . . . .
            . B . . . . . . .
            . . . . . . . . .
            W . B . . . . . .
            """, Player.White);
        var input = InputFor(p);
        input.Click(C("a1"));
        input.Click(C("b1"));
        Assert.Null(input.Completed);
        Assert.True(input.CanStopHere);
        Assert.Contains(C("b2"), input.ClickableCells);

        // Clicking the piece's current cell stops the chain there.
        input.Click(C("b1"));
        Assert.Equal("a1-b1A", Notation.Format(input.Completed!));

        // Continuing instead completes the full chain.
        input.Reset(p);
        input.Click(C("a1"));
        input.Click(C("b1"));
        input.Click(C("b2"));
        Assert.Equal("a1-b1A-b2A", Notation.Format(input.Completed!));
    }

    [Fact]
    public void ApproachWithdrawalAmbiguity_IsPendingUntilResolved()
    {
        // c3-d3 captures e3+f3 by approach or b3 by withdrawal.
        var p = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . B W . B B . . .
            . . . . . . . . .
            . . . . . . . . .
            """, Player.White);
        var input = InputFor(p);
        input.Click(C("c3"));
        input.Click(C("d3"));
        Assert.NotNull(input.Pending);
        Assert.Equal(Mask("e3", "f3"), input.Pending!.Value.Approach.Captured);
        Assert.Equal(Mask("b3"), input.Pending!.Value.Withdrawal.Captured);
        input.ChooseKind(CaptureKind.Withdrawal);
        Assert.Equal("c3-d3W", Notation.Format(input.Completed!));
        Assert.Equal(Mask("b3"), input.Completed!.CapturedMask);
    }

    [Fact]
    public void PaikaTwin_ClickAlwaysMeansTheCapture()
    {
        // Lenient candidates include the paika twin a1-b1, but AC removes captured pieces
        // automatically, so the click must resolve to the capture.
        var p = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            W . B . . . . . .
            """, Player.White);
        var input = InputFor(p);
        input.Click(C("a1"));
        input.Click(C("b1"));
        Assert.Equal("a1-b1A", Notation.Format(input.Completed!));
    }

    [Fact]
    public void LenientPaika_IsEnterableWhenItCapturesNothing()
    {
        // A capture exists elsewhere (a1-b1A), but AC's AI may move c5-c4 anyway.
        var p = Parse("""
            W . B . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            W . B . . . . . .
            """, Player.White);
        var input = InputFor(p);
        input.Click(C("a5"));
        input.Click(C("a4"));
        var turn = input.Completed!;
        Assert.Equal("a5-a4", Notation.Format(turn));
        Assert.True(MoveGenerator.IsLenientOnly(p, turn));
    }

    [Fact]
    public void UndoStep_WalksBackThroughPrefixAndOrigin()
    {
        var p = Parse("""
            . . . . . . . . .
            . B . . . . . . .
            . B . . . . . . .
            . . . . . . . . .
            W . B . . . . . .
            """, Player.White);
        var input = InputFor(p);
        input.Click(C("a1"));
        input.Click(C("b1"));
        Assert.Single(input.Path);
        input.UndoStep();
        Assert.Empty(input.Path);
        Assert.Equal(C("a1"), input.Origin);
        input.UndoStep();
        Assert.Null(input.Origin);
    }

    [Fact]
    public void ClickableCells_IdleListsExactlyTheMovablePieces()
    {
        var input = InputFor(Position.Initial);
        // Standard opening: every capture starts from d2, e2 or f3... derive from the generator.
        var expected = MoveGenerator.LenientTurns(Position.Initial).Select(t => t.Origin).Distinct().ToHashSet();
        Assert.Equal(expected, input.ClickableCells.ToHashSet());
    }

    [Fact]
    public void ClicksOffTheCandidateSet_AreIgnored()
    {
        var input = InputFor(Position.Initial);
        input.Click(C("i5")); // opponent piece: not an origin
        Assert.Null(input.Origin);
        input.Click(C("e2"));
        input.Click(C("i5")); // not a continuation
        Assert.Equal(C("e2"), input.Origin);
        Assert.Empty(input.Path);
        Assert.Null(input.Completed);
    }
}
