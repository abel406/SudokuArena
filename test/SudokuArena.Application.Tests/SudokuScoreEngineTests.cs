using SudokuArena.Application.Puzzles;
using SudokuArena.Application.Scoring;

namespace SudokuArena.Application.Tests;

public sealed class SudokuScoreEngineTests
{
    private readonly SudokuScoreEngine _engine = new();

    [Fact]
    public void ScoreMove_ShouldApplyFillClearAndNumberUseUp_OnCorrectMove()
    {
        var result = _engine.ScoreMove(new ScoreMoveInput(
            DifficultyTier.Hard,
            IsCorrectEditableFill: true,
            SecondsSinceLastCorrectFill: 3,
            CompletedUnitsCount: 2,
            NumberUsedUp: true));

        Assert.Equal(1175, result.OldDelta);
        Assert.Equal(287, result.NewDelta);
        Assert.Equal(825, result.OldFillDelta);
        Assert.Equal(140, result.OldClearDelta);
        Assert.Equal(210, result.OldNumberUseUpDelta);
        Assert.Equal(67, result.NewFillTimeDelta);
        Assert.Equal(10, result.NewClearDelta);
        Assert.Equal(210, result.NewNumberUseUpDelta);
    }

    [Fact]
    public void ScoreMove_ShouldApplyOnlyOldPenalty_OnInvalidMove()
    {
        var result = _engine.ScoreMove(new ScoreMoveInput(
            DifficultyTier.Expert,
            IsCorrectEditableFill: false,
            SecondsSinceLastCorrectFill: 1,
            CompletedUnitsCount: 0,
            NumberUsedUp: false));

        Assert.Equal(-640, result.OldDelta);
        Assert.Equal(0, result.NewDelta);
        Assert.Equal(-640, result.OldErrorPenaltyDelta);
    }

    [Fact]
    public void ScoreFinish_ShouldUseTimeBuckets_AndErrorRemaining()
    {
        var result = _engine.ScoreFinish(new ScoreFinishInput(
            DifficultyTier.Medium,
            ElapsedSeconds: 195,
            ErrorCount: 1,
            ErrorLimit: 3,
            IsPerfect: false,
            TimeThresholds: [120, 220, 320, 460]));

        Assert.Equal(8, result.TimeBucket);
        Assert.Equal(910, result.OldDelta);
        Assert.Equal(150, result.NewDelta);
        Assert.Equal(0, result.NewPerfectDelta);
        Assert.Equal(30, result.NewErrorDelta);
        Assert.Equal(120, result.NewTimeDelta);
    }

    [Fact]
    public void ResolveTimeBucket_ShouldReturnFallback_WhenThresholdsAreInvalid()
    {
        var bucket = _engine.ResolveTimeBucket(180, [100, 200, 300]);
        Assert.Equal(5, bucket);
    }

    [Fact]
    public void SelectFinalScore_ShouldPickVersionedValue()
    {
        Assert.Equal(1400, _engine.SelectFinalScore(1400, 420, ScoreVersion.Old));
        Assert.Equal(420, _engine.SelectFinalScore(1400, 420, ScoreVersion.New));
    }
}
