namespace SudokuArena.Application.Contracts.Theming;

public sealed record ThemeSnapshot(
    Guid ThemeId,
    string Code,
    string Name,
    int Version,
    bool IsPublished,
    bool IsActive,
    int Priority,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    IReadOnlyDictionary<string, string> Tokens,
    IReadOnlyDictionary<string, string> Assets,
    DateTimeOffset UpdatedUtc);
