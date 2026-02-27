using SudokuArena.Application.Puzzles;

namespace SudokuArena.Application.Scoring;

public sealed class SudokuScoreEngine
{
    public ScoreMoveResult ScoreMove(ScoreMoveInput input)
    {
        var profile = ResolveProfile(input.DifficultyTier);
        if (!input.IsCorrectEditableFill)
        {
            return new ScoreMoveResult(
                OldDelta: -profile.OldErrorPenalty,
                NewDelta: 0,
                OldFillDelta: 0,
                OldClearDelta: 0,
                OldNumberUseUpDelta: 0,
                OldErrorPenaltyDelta: -profile.OldErrorPenalty,
                NewFillTimeDelta: 0,
                NewClearDelta: 0,
                NewNumberUseUpDelta: 0);
        }

        var secondsWindow = Math.Clamp(input.SecondsSinceLastCorrectFill, 0, 12);
        var completedUnits = Math.Clamp(input.CompletedUnitsCount, 0, 3);
        var newFillDelta = Math.Max(0, profile.NewFillTimeBase - (profile.NewFillTimeRatio * secondsWindow));
        var newClearDelta = profile.NewClear * completedUnits;
        var newNumberUseUpDelta = input.NumberUsedUp ? profile.NumberUseUpBonus : 0;

        var oldFillDelta = Math.Max(0, profile.OldFillBase - (profile.OldFillRatio * secondsWindow));
        var oldClearDelta = profile.OldClear * completedUnits;
        var oldNumberUseUpDelta = input.NumberUsedUp ? profile.NumberUseUpBonus : 0;

        return new ScoreMoveResult(
            OldDelta: oldFillDelta + oldClearDelta + oldNumberUseUpDelta,
            NewDelta: newFillDelta + newClearDelta + newNumberUseUpDelta,
            OldFillDelta: oldFillDelta,
            OldClearDelta: oldClearDelta,
            OldNumberUseUpDelta: oldNumberUseUpDelta,
            OldErrorPenaltyDelta: 0,
            NewFillTimeDelta: newFillDelta,
            NewClearDelta: newClearDelta,
            NewNumberUseUpDelta: newNumberUseUpDelta);
    }

    public ScoreFinishResult ScoreFinish(ScoreFinishInput input)
    {
        var profile = ResolveProfile(input.DifficultyTier);
        var mistakesRemaining = Math.Max(0, input.ErrorLimit - input.ErrorCount);
        var timeBucket = ResolveTimeBucket(input.ElapsedSeconds, input.TimeThresholds);

        var newPerfect = input.IsPerfect ? profile.NewPerfect : 0;
        var newError = profile.NewError * mistakesRemaining;
        var newTime = profile.NewTime * timeBucket;

        var oldPerfect = input.IsPerfect ? profile.OldPerfect : 0;
        var oldError = profile.OldErrorBonus * mistakesRemaining;
        var oldTime = profile.OldTime * timeBucket;

        return new ScoreFinishResult(
            OldDelta: oldPerfect + oldError + oldTime,
            NewDelta: newPerfect + newError + newTime,
            OldPerfectDelta: oldPerfect,
            OldErrorDelta: oldError,
            OldTimeDelta: oldTime,
            NewPerfectDelta: newPerfect,
            NewErrorDelta: newError,
            NewTimeDelta: newTime,
            TimeBucket: timeBucket);
    }

    public int SelectFinalScore(int oldScore, int newScore, ScoreVersion scoreVersion)
    {
        return scoreVersion == ScoreVersion.New ? newScore : oldScore;
    }

    public int ResolveTimeBucket(int elapsedSeconds, IReadOnlyList<int>? timeThresholds)
    {
        if (timeThresholds is null || timeThresholds.Count != 4 || timeThresholds.Any(threshold => threshold <= 0))
        {
            return 5;
        }

        var elapsed = Math.Max(0, elapsedSeconds);
        if (elapsed <= timeThresholds[0])
        {
            return 10;
        }

        if (elapsed <= timeThresholds[1])
        {
            return 8;
        }

        if (elapsed <= timeThresholds[2])
        {
            return 6;
        }

        if (elapsed <= timeThresholds[3])
        {
            return 4;
        }

        return 2;
    }

    private static ScoreProfile ResolveProfile(DifficultyTier tier)
    {
        return tier switch
        {
            DifficultyTier.Beginner => new ScoreProfile(
                NewClear: 3,
                NewError: 10,
                NewPerfect: 20,
                NewTime: 10,
                NewFillTimeBase: 27,
                NewFillTimeRatio: 1,
                NumberUseUpBonus: 150,
                OldFillBase: 420,
                OldFillRatio: 15,
                OldClear: 30,
                OldPerfect: 250,
                OldErrorBonus: 80,
                OldTime: 60,
                OldErrorPenalty: 150),
            DifficultyTier.Easy => new ScoreProfile(
                NewClear: 3,
                NewError: 10,
                NewPerfect: 20,
                NewTime: 10,
                NewFillTimeBase: 27,
                NewFillTimeRatio: 1,
                NumberUseUpBonus: 150,
                OldFillBase: 420,
                OldFillRatio: 15,
                OldClear: 30,
                OldPerfect: 250,
                OldErrorBonus: 80,
                OldTime: 60,
                OldErrorPenalty: 150),
            DifficultyTier.Medium => new ScoreProfile(
                NewClear: 4,
                NewError: 15,
                NewPerfect: 30,
                NewTime: 15,
                NewFillTimeBase: 42,
                NewFillTimeRatio: 2,
                NumberUseUpBonus: 180,
                OldFillBase: 620,
                OldFillRatio: 20,
                OldClear: 45,
                OldPerfect: 420,
                OldErrorBonus: 95,
                OldTime: 90,
                OldErrorPenalty: 180),
            DifficultyTier.Hard => new ScoreProfile(
                NewClear: 5,
                NewError: 20,
                NewPerfect: 40,
                NewTime: 20,
                NewFillTimeBase: 76,
                NewFillTimeRatio: 3,
                NumberUseUpBonus: 210,
                OldFillBase: 900,
                OldFillRatio: 25,
                OldClear: 70,
                OldPerfect: 680,
                OldErrorBonus: 120,
                OldTime: 140,
                OldErrorPenalty: 400),
            DifficultyTier.Expert => new ScoreProfile(
                NewClear: 6,
                NewError: 25,
                NewPerfect: 50,
                NewTime: 25,
                NewFillTimeBase: 112,
                NewFillTimeRatio: 4,
                NumberUseUpBonus: 300,
                OldFillBase: 1300,
                OldFillRatio: 35,
                OldClear: 95,
                OldPerfect: 960,
                OldErrorBonus: 160,
                OldTime: 180,
                OldErrorPenalty: 640),
            _ => ResolveProfile(DifficultyTier.Medium)
        };
    }

    private sealed record ScoreProfile(
        int NewClear,
        int NewError,
        int NewPerfect,
        int NewTime,
        int NewFillTimeBase,
        int NewFillTimeRatio,
        int NumberUseUpBonus,
        int OldFillBase,
        int OldFillRatio,
        int OldClear,
        int OldPerfect,
        int OldErrorBonus,
        int OldTime,
        int OldErrorPenalty);
}

public sealed record ScoreMoveInput(
    DifficultyTier DifficultyTier,
    bool IsCorrectEditableFill,
    int SecondsSinceLastCorrectFill,
    int CompletedUnitsCount,
    bool NumberUsedUp);

public sealed record ScoreMoveResult(
    int OldDelta,
    int NewDelta,
    int OldFillDelta,
    int OldClearDelta,
    int OldNumberUseUpDelta,
    int OldErrorPenaltyDelta,
    int NewFillTimeDelta,
    int NewClearDelta,
    int NewNumberUseUpDelta);

public sealed record ScoreFinishInput(
    DifficultyTier DifficultyTier,
    int ElapsedSeconds,
    int ErrorCount,
    int ErrorLimit,
    bool IsPerfect,
    IReadOnlyList<int>? TimeThresholds);

public sealed record ScoreFinishResult(
    int OldDelta,
    int NewDelta,
    int OldPerfectDelta,
    int OldErrorDelta,
    int OldTimeDelta,
    int NewPerfectDelta,
    int NewErrorDelta,
    int NewTimeDelta,
    int TimeBucket);
