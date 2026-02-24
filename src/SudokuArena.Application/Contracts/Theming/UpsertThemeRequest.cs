namespace SudokuArena.Application.Contracts.Theming;

public sealed record UpsertThemeRequest(
    Guid? ThemeId,
    string Code,
    string Name,
    int? BaseVersion,
    bool IsPublished,
    int Priority,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    IReadOnlyDictionary<string, string>? Tokens,
    IReadOnlyDictionary<string, string>? Assets);
