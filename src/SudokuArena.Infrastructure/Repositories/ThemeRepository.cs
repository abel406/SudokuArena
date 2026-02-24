using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SudokuArena.Application.Abstractions.Repositories;
using SudokuArena.Domain.Models;
using SudokuArena.Infrastructure.Persistence;
using SudokuArena.Infrastructure.Persistence.Entities;

namespace SudokuArena.Infrastructure.Repositories;

public sealed class ThemeRepository(SudokuArenaDbContext dbContext) : IThemeRepository
{
    public async Task UpsertAsync(ThemeManifest theme, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Themes.SingleOrDefaultAsync(x => x.Id == theme.Id, cancellationToken);
        if (entity is null)
        {
            entity = new ThemeEntity { Id = theme.Id };
            dbContext.Themes.Add(entity);
        }

        entity.Code = theme.Code;
        entity.Name = theme.Name;
        entity.Version = theme.Version;
        entity.IsPublished = theme.IsPublished;
        entity.IsActive = theme.IsActive;
        entity.Priority = theme.Priority;
        entity.ValidFromUtc = theme.ValidFromUtc;
        entity.ValidToUtc = theme.ValidToUtc;
        entity.TokensJson = JsonSerializer.Serialize(theme.Tokens);
        entity.AssetsJson = JsonSerializer.Serialize(theme.Assets);
        entity.UpdatedUtc = theme.UpdatedUtc;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ThemeManifest?> GetByIdAsync(Guid themeId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Themes.SingleOrDefaultAsync(x => x.Id == themeId, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyList<ThemeManifest>> ListAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.Themes.ToListAsync(cancellationToken);

        return entities
            .Select(ToModel)
            .OrderByDescending(x => x.UpdatedUtc)
            .ToList();
    }

    public async Task<ThemeManifest?> GetActiveAsync(DateTimeOffset whenUtc, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Themes
            .Where(x => x.IsPublished && x.IsActive)
            .ToListAsync(cancellationToken);

        return entity
            .Select(ToModel)
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.UpdatedUtc)
            .FirstOrDefault(x => x.IsApplicableAt(whenUtc));
    }

    public async Task DeactivateAllAsync(CancellationToken cancellationToken)
    {
        var activeThemes = await dbContext.Themes
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var theme in activeThemes)
        {
            theme.IsActive = false;
            theme.UpdatedUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ThemeManifest ToModel(ThemeEntity entity)
    {
        var tokens = ParseDictionary(entity.TokensJson);
        var assets = ParseDictionary(entity.AssetsJson);

        return new ThemeManifest(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.Version,
            entity.IsPublished,
            entity.IsActive,
            entity.Priority,
            entity.ValidFromUtc,
            entity.ValidToUtc,
            tokens,
            assets,
            entity.UpdatedUtc);
    }

    private static IReadOnlyDictionary<string, string> ParseDictionary(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
               ?? new Dictionary<string, string>();
    }
}
