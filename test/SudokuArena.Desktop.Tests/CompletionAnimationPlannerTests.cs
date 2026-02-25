using SudokuArena.Desktop.Animations;

namespace SudokuArena.Desktop.Tests;

public sealed class CompletionAnimationPlannerTests
{
    [Fact]
    public void BuildBoardWaveDistances_ShouldCoverAllCells_FromCenter()
    {
        var distances = CompletionAnimationPlanner.BuildBoardWaveDistances(40);

        Assert.Equal(81, distances.Count);
        Assert.Equal(0, distances[40]);
        Assert.Equal(4, distances[0]);
        Assert.Equal(4, distances[80]);
    }

    [Fact]
    public void BuildBoardWaveDistances_ShouldUseChebyshevRings_FromCorner()
    {
        var distances = CompletionAnimationPlanner.BuildBoardWaveDistances(0);

        Assert.Equal(81, distances.Count);
        Assert.Equal(0, distances[0]);
        Assert.Equal(1, distances[1]);
        Assert.Equal(1, distances[9]);
        Assert.Equal(8, distances[80]);
    }

    [Fact]
    public void BuildDistances_ShouldReturnEmpty_WhenNoUnitsCompleted()
    {
        var distances = CompletionAnimationPlanner.BuildDistances(40, false, false, false);

        Assert.Empty(distances);
    }

    [Fact]
    public void BuildDistances_ShouldCoverFullRow_WhenRowCompleted()
    {
        var distances = CompletionAnimationPlanner.BuildDistances(40, true, false, false);

        Assert.Equal(9, distances.Count);
        for (var col = 0; col < 9; col++)
        {
            var index = (4 * 9) + col;
            Assert.True(distances.ContainsKey(index));
            Assert.Equal(Math.Abs(col - 4), distances[index]);
        }
    }

    [Fact]
    public void BuildDistances_ShouldCoverFullBox_WhenBoxCompleted()
    {
        var distances = CompletionAnimationPlanner.BuildDistances(40, false, false, true);

        Assert.Equal(9, distances.Count);
        var expectedBox = new[]
        {
            30, 31, 32,
            39, 40, 41,
            48, 49, 50
        };

        foreach (var index in expectedBox)
        {
            Assert.True(distances.ContainsKey(index));
        }
    }

    [Fact]
    public void BuildDistances_ShouldMergeUnitsWithoutDuplicates_WhenRowAndBoxCompleted()
    {
        var distances = CompletionAnimationPlanner.BuildDistances(40, true, false, true);

        Assert.Equal(15, distances.Count);
        Assert.Equal(0, distances[40]);
        Assert.True(distances.ContainsKey(30)); // box only
        Assert.True(distances.ContainsKey(36)); // row only
    }
}
