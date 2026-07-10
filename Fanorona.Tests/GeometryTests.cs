using Fanorona.Core;
using static Fanorona.Tests.TestBoards;

namespace Fanorona.Tests;

public class GeometryTests
{
    private static List<Cell> Neighbors(string cell)
    {
        var result = new List<Cell>();
        for (var dir = 0; dir < Board.DirectionCount; dir++)
        {
            var n = Board.Neighbor(C(cell).Index, dir);
            if (n >= 0)
                result.Add(new Cell(n));
        }
        return result;
    }

    private static void AssertNeighbors(string cell, params string[] expected) =>
        Assert.Equal(expected.Select(C).OrderBy(c => c.Index), Neighbors(cell).OrderBy(c => c.Index));

    [Fact]
    public void CornerA1_IsStrong_WithThreeNeighbors()
    {
        Assert.True(Board.IsStrong(C("a1").Index));
        AssertNeighbors("a1", "b1", "a2", "b2");
    }

    [Fact]
    public void EdgeB1_IsWeak_WithThreeNeighbors()
    {
        Assert.False(Board.IsStrong(C("b1").Index));
        AssertNeighbors("b1", "a1", "c1", "b2");
    }

    [Fact]
    public void InteriorB3_IsWeak_WithFourNeighbors()
    {
        Assert.False(Board.IsStrong(C("b3").Index));
        AssertNeighbors("b3", "a3", "c3", "b2", "b4");
    }

    [Fact]
    public void CenterE3_IsStrong_WithEightNeighbors()
    {
        Assert.True(Board.IsStrong(C("e3").Index));
        AssertNeighbors("e3", "d3", "f3", "e2", "e4", "d2", "f2", "d4", "f4");
    }

    [Fact]
    public void Adjacency_IsSymmetric()
    {
        for (var cell = 0; cell < Board.Cells; cell++)
        {
            for (var dir = 0; dir < Board.DirectionCount; dir++)
            {
                var n = Board.Neighbor(cell, dir);
                if (n >= 0)
                    Assert.Equal(cell, Board.Neighbor(n, Board.Opposite(dir)));
            }
        }
    }
}
