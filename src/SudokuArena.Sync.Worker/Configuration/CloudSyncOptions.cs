namespace SudokuArena.Sync.Worker.Configuration;

public sealed class CloudSyncOptions
{
    public const string SectionName = "CloudSync";

    public string BaseUrl { get; init; } = string.Empty;

    public string ApiToken { get; init; } = string.Empty;

    public int PollSeconds { get; init; } = 5;
}
