using Fanorona.Core;
using static Fanorona.Tests.TestBoards;

namespace Fanorona.Tests;

public class NotationTests
{
    private static readonly List<Turn> OpeningTurns = MoveGenerator.LegalTurns(Position.Initial);

    [Fact]
    public void CanonicalForm_RoundTrips()
    {
        foreach (var turn in OpeningTurns)
        {
            var result = Assert.IsType<ParseResult.Matched>(
                Notation.Parse(Notation.Format(turn), OpeningTurns));
            Assert.Equal(turn, result.Turn);
        }
    }

    [Fact]
    public void OmittedSuffix_MatchesWhenUnambiguous()
    {
        var result = Assert.IsType<ParseResult.Matched>(Notation.Parse("e2-e3", OpeningTurns));
        Assert.Equal("e2-e3A", Notation.Format(result.Turn));
    }

    [Fact]
    public void OmittedSuffix_IsAmbiguousWhenBothCapturesExist()
    {
        var result = Assert.IsType<ParseResult.Ambiguous>(Notation.Parse("d3-e3", OpeningTurns));
        Assert.Equal(new[] { "d3-e3A", "d3-e3W" }, Notations(result.Options));
    }

    [Fact]
    public void Parsing_ToleratesCaseSpacesAndMissingDashes()
    {
        Assert.IsType<ParseResult.Matched>(Notation.Parse("D3 E3 W", OpeningTurns));
        Assert.IsType<ParseResult.Matched>(Notation.Parse("d3e3a", OpeningTurns));
        Assert.IsType<ParseResult.Matched>(Notation.Parse("e2e3", OpeningTurns));
    }

    [Fact]
    public void DashlessInput_ReadsCellsStartingWithA()
    {
        var p = Parse("""
            . . . . . . . . .
            . . . . . . . . .
            . . . . . . . . .
            . W . . . . . . .
            . . . . . . . . B
            """, Player.White);
        var turns = MoveGenerator.LegalTurns(p);
        var result = Assert.IsType<ParseResult.Matched>(Notation.Parse("b2a3", turns));
        Assert.Equal("b2-a3", Notation.Format(result.Turn));
    }

    [Fact]
    public void CaptureMatch_IsPreferredOverItsLenientPaikaTwin()
    {
        var lenient = MoveGenerator.LenientTurns(Position.Initial);
        var result = Assert.IsType<ParseResult.Matched>(Notation.Parse("e2-e3", lenient));
        Assert.Equal("e2-e3A", Notation.Format(result.Turn));
    }

    [Fact]
    public void ChainNotation_ParsesWithAndWithoutSuffixes()
    {
        var opening = OpeningTurns.Single(t => Notation.Format(t) == "e2-e3A");
        var replies = MoveGenerator.LegalTurns(Position.Initial.Apply(opening));

        var chain = Assert.IsType<ParseResult.Matched>(Notation.Parse("f4e5e4", replies));
        Assert.Equal("f4-e5W-e4A", Notation.Format(chain.Turn));

        var single = Assert.IsType<ParseResult.Matched>(Notation.Parse("f4-e5", replies));
        Assert.Equal("f4-e5W", Notation.Format(single.Turn));
    }

    [Theory]
    [InlineData("e2")]         // only one point
    [InlineData("zz-e3")]      // not a cell
    [InlineData("e2-e6")]      // rank out of range
    [InlineData("e2A-e3")]     // capture marker on the starting point
    [InlineData("a1-b1")]      // geometrically fine but not a legal turn
    public void BadInput_IsRejected(string input)
    {
        Assert.IsType<ParseResult.NoMatch>(Notation.Parse(input, OpeningTurns));
    }
}
