namespace SudokuArena.Infrastructure.Configuration;

public sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";

    public string RootPath { get; init; } = "./media";
}
