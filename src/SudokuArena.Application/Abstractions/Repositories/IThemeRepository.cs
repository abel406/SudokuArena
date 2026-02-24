using SudokuArena.Domain.Models;

namespace SudokuArena.Application.Abstractions.Repositories;

public interface IThemeRepository
{
    Task UpsertAsync(ThemeManifest theme, CancellationToken cancellationToken);

    Task<ThemeManifest?> GetByIdAsync(Guid themeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ThemeManifest>> ListAsync(CancellationToken cancellationToken);

    Task<ThemeManifest?> GetActiveAsync(DateTimeOffset whenUtc, CancellationToken cancellationToken);

    Task DeactivateAllAsync(CancellationToken cancellationToken);
}
