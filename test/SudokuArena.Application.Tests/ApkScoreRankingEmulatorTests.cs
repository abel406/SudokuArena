using SudokuArena.Application.Scoring;

namespace SudokuArena.Application.Tests;

public sealed class ApkScoreRankingEmulatorTests
{
    private readonly ApkScoreRankingEmulator _emulator = new();

    [Fact]
    public void ComputeV1WindowSummary_ShouldMatchKnownTypeAndScoreRules()
    {
        var start = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        var games = new[]
        {
            Game("p1", "2026-02-10T10:00:00+00:00", ApkGameState.Completed, ApkGameType.Normal, 0, 10),
            Game("p1", "2026-02-10T11:00:00+00:00", ApkGameState.Completed, ApkGameType.Dc, 0, 0),
            Game("p1", "2026-02-10T12:00:00+00:00", ApkGameState.Completed, ApkGameType.Active, 0, -5),
            Game("p1", "2026-02-10T13:00:00+00:00", ApkGameState.Completed, ApkGameType.Battle, 0, 500),
            Game("p1", "2026-02-10T14:00:00+00:00", 10, ApkGameType.Normal, 0, 100),
            Game("p1", "2026-03-10T10:00:00+00:00", ApkGameState.Completed, ApkGameType.Normal, 0, 100)
        };

        var summary = _emulator.ComputeV1WindowSummary(games, start, end);

        Assert.Equal(2, summary.MatchedGames);
        Assert.Equal(10, summary.TotalOldScore);
    }

    [Fact]
    public void ComputeW1WindowSummary_ShouldUsePositiveScoreAndDefaultTypeRules()
    {
        var start = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        var games = new[]
        {
            Game("p1", "2026-02-10T10:00:00+00:00", ApkGameState.Completed, ApkGameType.Normal, 0, 10),
            Game("p1", "2026-02-10T11:00:00+00:00", ApkGameState.Completed, ApkGameType.Dc, 0, 20),
            Game("p1", "2026-02-10T12:00:00+00:00", ApkGameState.Completed, ApkGameType.Active, 0, 30),
            Game("p1", "2026-02-10T12:30:00+00:00", ApkGameState.Completed, ApkGameType.Normal, 0, 0),
            Game("p1", "2026-02-10T13:00:00+00:00", ApkGameState.Completed, ApkGameType.Battle, 0, 40),
            Game("p1", "2026-02-10T14:00:00+00:00", ApkGameState.Completed, ApkGameType.Daily, 1, 50),
            Game("p1", "2026-02-10T15:00:00+00:00", ApkGameState.Completed, ApkGameType.Daily, 3, 60)
        };

        var summary = _emulator.ComputeW1WindowSummary(games, start, end);

        Assert.Equal(3, summary.MatchedGames);
        Assert.Equal(60, summary.TotalOldScore);
    }

    [Fact]
    public void ComputeW1WindowSummary_ShouldAllowBattleAndExploreModes_WhenConfigured()
    {
        var start = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        var games = new[]
        {
            Game("p1", "2026-02-10T10:00:00+00:00", ApkGameState.Completed, ApkGameType.Battle, 0, 40),
            Game("p1", "2026-02-10T14:00:00+00:00", ApkGameState.Completed, ApkGameType.Daily, 1, 50)
        };

        var summary = _emulator.ComputeW1WindowSummary(
            games,
            start,
            end,
            new ApkRankingEmulationOptions(
                IsBattleScoreTypeMode: gameType => gameType == ApkGameType.Battle,
                IsExploreScoreTypeMode: (gameType, sudokuType) => gameType == ApkGameType.Daily && sudokuType == 1));

        Assert.Equal(2, summary.MatchedGames);
        Assert.Equal(90, summary.TotalOldScore);
    }

    [Fact]
    public void BuildW1Leaderboard_ShouldSortAndAssignDenseRankByScore()
    {
        var start = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        var games = new[]
        {
            Game("p1", "2026-02-10T10:00:00+00:00", ApkGameState.Completed, ApkGameType.Normal, 0, 120),
            Game("p1", "2026-02-10T11:00:00+00:00", ApkGameState.Completed, ApkGameType.Normal, 0, 80),
            Game("p2", "2026-02-10T12:00:00+00:00", ApkGameState.Completed, ApkGameType.Normal, 0, 200),
            Game("p3", "2026-02-10T13:00:00+00:00", ApkGameState.Completed, ApkGameType.Normal, 0, 150)
        };

        var leaderboard = _emulator.BuildW1Leaderboard(games, start, end);

        Assert.Collection(
            leaderboard,
            entry =>
            {
                Assert.Equal("p1", entry.PlayerId);
                Assert.Equal(2, entry.MatchedGames);
                Assert.Equal(200, entry.TotalOldScore);
                Assert.Equal(1, entry.Rank);
            },
            entry =>
            {
                Assert.Equal("p2", entry.PlayerId);
                Assert.Equal(1, entry.MatchedGames);
                Assert.Equal(200, entry.TotalOldScore);
                Assert.Equal(1, entry.Rank);
            },
            entry =>
            {
                Assert.Equal("p3", entry.PlayerId);
                Assert.Equal(1, entry.MatchedGames);
                Assert.Equal(150, entry.TotalOldScore);
                Assert.Equal(2, entry.Rank);
            });
    }

    [Fact]
    public void IsOldScoring_ShouldMatchNullOrZeroRule()
    {
        Assert.True(_emulator.IsOldScoring(null));
        Assert.True(_emulator.IsOldScoring(0));
        Assert.False(_emulator.IsOldScoring(1));
    }

    private static ApkScoreGame Game(
        string playerId,
        string lastOperationUtc,
        int state,
        int gameType,
        int sudokuType,
        int oldScore)
    {
        return new ApkScoreGame(
            PlayerId: playerId,
            LastOperationUtc: DateTimeOffset.Parse(lastOperationUtc),
            State: state,
            GameType: gameType,
            SudokuType: sudokuType,
            OldScore: oldScore,
            NewScore: null,
            ScoreVersion: null);
    }
}
