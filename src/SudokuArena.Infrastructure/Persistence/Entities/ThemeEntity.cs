namespace SudokuArena.Infrastructure.Persistence.Entities;

public sealed class ThemeEntity
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Version { get; set; }

    public bool IsPublished { get; set; }

    public bool IsActive { get; set; }

    public int Priority { get; set; }

    public DateTimeOffset? ValidFromUtc { get; set; }

    public DateTimeOffset? ValidToUtc { get; set; }

    public string TokensJson { get; set; } = "{}";

    public string AssetsJson { get; set; } = "{}";

    public DateTimeOffset UpdatedUtc { get; set; }
}
