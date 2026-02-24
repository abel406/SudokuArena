using SudokuArena.Application.Contracts.Theming;

namespace SudokuArena.Application.Abstractions.Services;

public interface IThemeService
{
    Task<ThemeSnapshot> UpsertAsync(UpsertThemeRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ThemeSnapshot>> ListAsync(CancellationToken cancellationToken);

    Task<ThemeSnapshot?> GetActiveAsync(CancellationToken cancellationToken);

    Task<ThemeSnapshot?> ActivateAsync(Guid themeId, CancellationToken cancellationToken);
}
