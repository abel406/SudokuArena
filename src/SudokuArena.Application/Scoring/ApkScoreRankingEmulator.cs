namespace SudokuArena.Application.Scoring;

public static class ApkGameState
{
    public const int Completed = 15;
}

public static class ApkGameType
{
    public const int Normal = 0;
    public const int Dc = 1;
    public const int Active = 2;
    public const int Battle = 3;
    public const int Daily = 4;
}

public sealed record ApkScoreGame(
    string PlayerId,
    DateTimeOffset LastOperationUtc,
    int State,
    int GameType,
    int SudokuType,
    int? OldScore,
    int? NewScore,
    int? ScoreVersion);

public sealed record ApkScoreWindowSummary(int MatchedGames, int TotalOldScore);

public sealed record ApkLeaderboardEntry(
    string PlayerId,
    int MatchedGames,
    int TotalOldScore,
    int Rank);

public sealed record ApkRankingEmulationOptions(
    Func<int, bool>? IsBattleScoreTypeMode = null,
    Func<int, int, bool>? IsExploreScoreTypeMode = null);

public sealed class ApkScoreRankingEmulator
{
    public ApkScoreWindowSummary ComputeV1WindowSummary(
        IEnumerable<ApkScoreGame> games,
        DateTimeOffset startUtcExclusive,
        DateTimeOffset endUtcExclusive)
    {
        var candidates = games.Where(game =>
            game.State == ApkGameState.Completed &&
            game.LastOperationUtc > startUtcExclusive &&
            game.LastOperationUtc < endUtcExclusive &&
            (game.GameType is ApkGameType.Normal or ApkGameType.Dc or ApkGameType.Active or ApkGameType.Battle));

        var matchedGames = 0;
        var totalOldScore = 0;

        foreach (var game in candidates)
        {
            if (game.OldScore is null)
            {
                continue;
            }

            var z10 = true;
            var z11 = game.GameType == ApkGameType.Normal;
            if (game.GameType != ApkGameType.Dc && game.GameType != ApkGameType.Active)
            {
                z10 = z11;
            }

            if (game.OldScore.Value >= 0 && z10)
            {
                totalOldScore += game.OldScore.Value;
                matchedGames++;
            }
        }

        return new ApkScoreWindowSummary(matchedGames, totalOldScore);
    }

    public ApkScoreWindowSummary ComputeW1WindowSummary(
        IEnumerable<ApkScoreGame> games,
        DateTimeOffset startUtcExclusive,
        DateTimeOffset endUtcExclusive,
        ApkRankingEmulationOptions? options = null)
    {
        options ??= new ApkRankingEmulationOptions();
        var isBattleTypeMode = options.IsBattleScoreTypeMode ?? (_ => false);
        var isExploreTypeMode = options.IsExploreScoreTypeMode ?? ((_, _) => false);

        var candidates = games.Where(game =>
            game.State == ApkGameState.Completed &&
            game.LastOperationUtc > startUtcExclusive &&
            game.LastOperationUtc < endUtcExclusive &&
            (game.GameType is ApkGameType.Normal or ApkGameType.Dc or ApkGameType.Active or ApkGameType.Battle ||
             (game.GameType == ApkGameType.Daily && (game.SudokuType is 1 or 2))));

        var matchedGames = 0;
        var totalOldScore = 0;

        foreach (var game in candidates)
        {
            if (game.OldScore is null)
            {
                continue;
            }

            var z10 = true;
            var z11 = game.GameType == ApkGameType.Normal ||
                      isBattleTypeMode(game.GameType) ||
                      isExploreTypeMode(game.GameType, game.SudokuType);
            if (game.GameType != ApkGameType.Dc && game.GameType != ApkGameType.Active)
            {
                z10 = z11;
            }

            if (game.OldScore.Value > 0 && z10)
            {
                totalOldScore += game.OldScore.Value;
                matchedGames++;
            }
        }

        return new ApkScoreWindowSummary(matchedGames, totalOldScore);
    }

    public IReadOnlyList<ApkLeaderboardEntry> BuildW1Leaderboard(
        IEnumerable<ApkScoreGame> games,
        DateTimeOffset startUtcExclusive,
        DateTimeOffset endUtcExclusive,
        ApkRankingEmulationOptions? options = null)
    {
        var perPlayer = games
            .GroupBy(game => game.PlayerId)
            .Select(group =>
            {
                var summary = ComputeW1WindowSummary(group, startUtcExclusive, endUtcExclusive, options);
                return (PlayerId: group.Key, Summary: summary);
            })
            .Where(item => item.Summary.MatchedGames > 0)
            .OrderByDescending(item => item.Summary.TotalOldScore)
            .ThenByDescending(item => item.Summary.MatchedGames)
            .ThenBy(item => item.PlayerId, StringComparer.Ordinal)
            .ToList();

        var rank = 0;
        int? previousScore = null;
        var entries = new List<ApkLeaderboardEntry>(perPlayer.Count);

        foreach (var item in perPlayer)
        {
            if (previousScore != item.Summary.TotalOldScore)
            {
                rank++;
                previousScore = item.Summary.TotalOldScore;
            }

            entries.Add(new ApkLeaderboardEntry(
                item.PlayerId,
                item.Summary.MatchedGames,
                item.Summary.TotalOldScore,
                rank));
        }

        return entries;
    }

    public bool IsOldScoring(int? scoreVersion)
    {
        return scoreVersion is null or 0;
    }
}
