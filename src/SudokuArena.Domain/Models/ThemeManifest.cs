namespace SudokuArena.Domain.Models;

public sealed class ThemeManifest
{
    public ThemeManifest(
        Guid id,
        string code,
        string name,
        int version,
        bool isPublished,
        bool isActive,
        int priority,
        DateTimeOffset? validFromUtc,
        DateTimeOffset? validToUtc,
        IReadOnlyDictionary<string, string> tokens,
        IReadOnlyDictionary<string, string> assets,
        DateTimeOffset updatedUtc)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Theme code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Theme name is required.", nameof(name));
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Theme version must be greater than zero.");
        }

        if (validFromUtc is not null && validToUtc is not null && validFromUtc > validToUtc)
        {
            throw new ArgumentException("ValidFromUtc cannot be after ValidToUtc.");
        }

        Id = id;
        Code = code.Trim();
        Name = name.Trim();
        Version = version;
        IsPublished = isPublished;
        IsActive = isActive;
        Priority = priority;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        Tokens = tokens;
        Assets = assets;
        UpdatedUtc = updatedUtc;
    }

    public Guid Id { get; }

    public string Code { get; }

    public string Name { get; }

    public int Version { get; }

    public bool IsPublished { get; }

    public bool IsActive { get; }

    public int Priority { get; }

    public DateTimeOffset? ValidFromUtc { get; }

    public DateTimeOffset? ValidToUtc { get; }

    public IReadOnlyDictionary<string, string> Tokens { get; }

    public IReadOnlyDictionary<string, string> Assets { get; }

    public DateTimeOffset UpdatedUtc { get; }

    public bool IsApplicableAt(DateTimeOffset whenUtc)
    {
        if (!IsPublished || !IsActive)
        {
            return false;
        }

        if (ValidFromUtc is not null && whenUtc < ValidFromUtc.Value)
        {
            return false;
        }

        if (ValidToUtc is not null && whenUtc > ValidToUtc.Value)
        {
            return false;
        }

        return true;
    }

    public ThemeManifest Activate(DateTimeOffset updatedUtc)
    {
        return new ThemeManifest(
            Id,
            Code,
            Name,
            Version,
            IsPublished,
            true,
            Priority,
            ValidFromUtc,
            ValidToUtc,
            Tokens,
            Assets,
            updatedUtc);
    }

    public ThemeManifest Deactivate(DateTimeOffset updatedUtc)
    {
        return new ThemeManifest(
            Id,
            Code,
            Name,
            Version,
            IsPublished,
            false,
            Priority,
            ValidFromUtc,
            ValidToUtc,
            Tokens,
            Assets,
            updatedUtc);
    }
}
