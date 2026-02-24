using SudokuArena.Application.Abstractions.Repositories;
using SudokuArena.Application.Abstractions.Services;
using SudokuArena.Application.Contracts.Theming;
using SudokuArena.Domain.Models;

namespace SudokuArena.Application.Services;

public sealed class ThemeService(IThemeRepository themeRepository) : IThemeService
{
    public async Task<ThemeSnapshot> UpsertAsync(UpsertThemeRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var existing = request.ThemeId is null
            ? null
            : await themeRepository.GetByIdAsync(request.ThemeId.Value, cancellationToken);

        var themeId = request.ThemeId ?? Guid.NewGuid();
        var version = existing is null ? 1 : Math.Max(existing.Version + 1, request.BaseVersion ?? 1);
        var tokens = request.Tokens ?? existing?.Tokens ?? new Dictionary<string, string>();
        var assets = request.Assets ?? existing?.Assets ?? new Dictionary<string, string>();

        var theme = new ThemeManifest(
            themeId,
            request.Code,
            request.Name,
            version,
            request.IsPublished,
            existing?.IsActive ?? false,
            request.Priority,
            request.ValidFromUtc,
            request.ValidToUtc,
            tokens,
            assets,
            now);

        await themeRepository.UpsertAsync(theme, cancellationToken);
        return ToSnapshot(theme);
    }

    public async Task<IReadOnlyList<ThemeSnapshot>> ListAsync(CancellationToken cancellationToken)
    {
        var themes = await themeRepository.ListAsync(cancellationToken);
        return themes
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.Priority)
            .ThenByDescending(x => x.UpdatedUtc)
            .Select(ToSnapshot)
            .ToList();
    }

    public async Task<ThemeSnapshot?> GetActiveAsync(CancellationToken cancellationToken)
    {
        var active = await themeRepository.GetActiveAsync(DateTimeOffset.UtcNow, cancellationToken);
        return active is null ? null : ToSnapshot(active);
    }

    public async Task<ThemeSnapshot?> ActivateAsync(Guid themeId, CancellationToken cancellationToken)
    {
        var theme = await themeRepository.GetByIdAsync(themeId, cancellationToken);
        if (theme is null)
        {
            return null;
        }

        await themeRepository.DeactivateAllAsync(cancellationToken);
        var activated = theme.Activate(DateTimeOffset.UtcNow);
        await themeRepository.UpsertAsync(activated, cancellationToken);

        return ToSnapshot(activated);
    }

    private static ThemeSnapshot ToSnapshot(ThemeManifest theme)
    {
        return new ThemeSnapshot(
            theme.Id,
            theme.Code,
            theme.Name,
            theme.Version,
            theme.IsPublished,
            theme.IsActive,
            theme.Priority,
            theme.ValidFromUtc,
            theme.ValidToUtc,
            theme.Tokens,
            theme.Assets,
            theme.UpdatedUtc);
    }
}
