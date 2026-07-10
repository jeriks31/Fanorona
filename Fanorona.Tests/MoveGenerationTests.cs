using Fanorona.Core;
using static Fanorona.Tests.TestBoards;

namespace Fanorona.Tests;

public class MoveGenerationTests
{
    [Fact]
    public void Chain_EmitsEveryLegalStoppingPoint()
    {
        var p = Parse("""
            . . . . . . . . .
            . B . . . . . . .
            . B . . . . . . .
            . . . . . . . . .
            W . B . . . . . .
            """, Player.White);
        var turns = MoveGenerator.LegalTurns(p);
        Assert.Equal(new[] { "a1-b1A", "a1-b1A-b2A" }, Notations(turns));
        var chain = turns.Single(t => t.Segments.Length == 2);
        Assert.Equal(Mask("c1", "b3", "b4"), chain.CapturedMask);
    }

    [Fact]
    public void Chain_MayNotRevisitTheStartingCell()
    {
        // After d3-c3A (capturing b3), continuing east back to d3 would capture e3+f3,
        // but d3 is the turn's starting cell, so no two-segment turn may exist.
        var p = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . B . W B B . . .
            . . . . . . . . .
            . . . . . . . . .
            """, Player.White);
        var turns = MoveGenerator.LegalTurns(p);
        Assert.Equal(new[] { "d3-c3A", "d3-c3W" }, Notations(turns));
    }

    [Fact]
    public void Chain_MayNotRepeatDirectionConsecutively()
    {
        // After e3-d3W (capturing f3, g3), continuing west to c3 would capture b3 by
        // approach, but that repeats the direction of the previous segment.
        var p = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . B . . W B B . .
            . . . . . . . . .
            . . . . . . . . .
            """, Player.White);
        var turn = Assert.Single(MoveGenerator.LegalTurns(p));
        Assert.Equal("e3-d3W", Notation.Format(turn));
    }

    [Fact]
    public void Chain_MayReuseADirectionNonConsecutively()
    {
        // North, east, north: legal because the repeat is not consecutive.
        var p = Parse("""
            . . . . . . . . .
            . . . B . . . . .
            . . B . . . . . .
            . . . . B . . . .
            . . W . . . . . .
            """, Player.White);
        var turns = MoveGenerator.LegalTurns(p);
        Assert.Equal(new[] { "c1-c2A", "c1-c2A-d2A", "c1-c2A-d2A-d3A" }, Notations(turns));
    }

    [Fact]
    public void Chain_CapturedPiecesAreRemovedSegmentBySegment()
    {
        // Segment 3 lands on c3, which is only empty because segment 1 captured it.
        var p = Parse("""
            . . . . . . . . .
            . B B . . . . . .
            . . B . . . . . .
            . . . . B . . . .
            . . W . . . . . .
            """, Player.White);
        var turns = MoveGenerator.LegalTurns(p);
        Assert.Equal(new[] { "c1-c2A", "c1-c2A-d2A", "c1-c2A-d2A-c3A" }, Notations(turns));
        var chain = turns.Single(t => t.Segments.Length == 3);
        Assert.Equal(Mask("c3", "c4", "e2", "b4"), chain.CapturedMask);
    }

    [Fact]
    public void BlackRepliesToTheStandardOpening_AreExactlyTwoTurns()
    {
        var opening = MoveGenerator.LegalTurns(Position.Initial)
            .Single(t => Notation.Format(t) == "e2-e3A");
        var p = Position.Initial.Apply(opening);
        var turns = MoveGenerator.LegalTurns(p);
        Assert.Equal(new[] { "f4-e5W", "f4-e5W-e4A" }, Notations(turns));
        var byNotation = turns.ToDictionary(Notation.Format);
        Assert.Equal(Mask("g3", "h2", "i1"), byNotation["f4-e5W"].CapturedMask);
        Assert.Equal(Mask("g3", "h2", "i1", "e3"), byNotation["f4-e5W-e4A"].CapturedMask);
    }

    [Fact]
    public void RandomPlayouts_PreserveRuleInvariants()
    {
        var random = new Random(12345);
        for (var game = 0; game < 40; game++)
        {
            var position = Position.Initial;
            for (var ply = 0; ply < 120; ply++)
            {
                var turns = MoveGenerator.LegalTurns(position);
                if (turns.Count == 0)
                    break;
                var anyCapture = turns.Any(t => t.IsCapture);
                foreach (var turn in turns)
                {
                    // Mandatory capture: paika turns never coexist with captures.
                    Assert.Equal(anyCapture, turn.IsCapture);
                    var path = PathCells(turn);
                    Assert.Equal(path.Count, path.Distinct().Count());
                    for (var i = 0; i < turn.Segments.Length; i++)
                    {
                        if (i > 0)
                        {
                            Assert.NotEqual(CaptureKind.None, turn.Segments[i].Kind);
                            Assert.NotEqual(Direction(turn.Segments[i - 1]), Direction(turn.Segments[i]));
                        }
                        Assert.Equal(turn.Segments[i].Kind != CaptureKind.None,
                            turn.Segments[i].Captured != 0);
                    }
                    var enemy = position.Pieces(position.ToMove.Opponent());
                    Assert.Equal(turn.CapturedMask, turn.CapturedMask & enemy);
                    var after = position.Apply(turn);
                    Assert.Equal(0UL, after.White & after.Black);
                    Assert.Equal(position.PieceCount(position.ToMove),
                        after.PieceCount(position.ToMove));
                    Assert.Equal(position.PieceCount(position.ToMove.Opponent()) - turn.CapturedCount,
                        after.PieceCount(position.ToMove.Opponent()));
                }
                position = position.Apply(turns[random.Next(turns.Count)]);
            }
        }
    }
}
