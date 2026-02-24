using SudokuArena.Application.Abstractions.Repositories;
using SudokuArena.Application.Contracts.Theming;
using SudokuArena.Application.Services;
using SudokuArena.Domain.Models;

namespace SudokuArena.Application.Tests;

public sealed class ThemeServiceTests
{
    [Fact]
    public async Task ActivateAsync_ShouldDeactivatePreviousActiveTheme()
    {
        var repository = new FakeThemeRepository();
        var now = DateTimeOffset.UtcNow;

        var first = new ThemeManifest(
            Guid.NewGuid(),
            "default",
            "Default",
            1,
            true,
            true,
            10,
            now.AddDays(-1),
            now.AddDays(1),
            new Dictionary<string, string> { ["color.primary"] = "#111111" },
            new Dictionary<string, string>(),
            now);

        var second = new ThemeManifest(
            Guid.NewGuid(),
            "halloween",
            "Halloween",
            1,
            true,
            false,
            20,
            now.AddDays(-1),
            now.AddDays(1),
            new Dictionary<string, string> { ["color.primary"] = "#ff6a00" },
            new Dictionary<string, string>(),
            now);

        await repository.UpsertAsync(first, CancellationToken.None);
        await repository.UpsertAsync(second, CancellationToken.None);

        var service = new ThemeService(repository);
        var activated = await service.ActivateAsync(second.Id, CancellationToken.None);

        Assert.NotNull(activated);
        Assert.Equal(second.Id, activated!.ThemeId);

        var active = await service.GetActiveAsync(CancellationToken.None);
        Assert.NotNull(active);
        Assert.Equal(second.Id, active!.ThemeId);
    }

    [Fact]
    public async Task UpsertAsync_ShouldIncreaseVersionWhenThemeAlreadyExists()
    {
        var repository = new FakeThemeRepository();
        var now = DateTimeOffset.UtcNow;
        var existingId = Guid.NewGuid();

        await repository.UpsertAsync(
            new ThemeManifest(
                existingId,
                "winter",
                "Winter",
                1,
                true,
                false,
                5,
                now.AddDays(-10),
                now.AddDays(10),
                new Dictionary<string, string>(),
                new Dictionary<string, string>(),
                now),
            CancellationToken.None);

        var service = new ThemeService(repository);
        var updated = await service.UpsertAsync(
            new UpsertThemeRequest(
                existingId,
                "winter",
                "Winter Updated",
                null,
                true,
                5,
                now.AddDays(-10),
                now.AddDays(10),
                new Dictionary<string, string> { ["color.primary"] = "#00ffff" },
                new Dictionary<string, string>()),
            CancellationToken.None);

        Assert.Equal(2, updated.Version);
        Assert.Equal("Winter Updated", updated.Name);
    }

    private sealed class FakeThemeRepository : IThemeRepository
    {
        private readonly Dictionary<Guid, ThemeManifest> _themes = new();

        public Task UpsertAsync(ThemeManifest theme, CancellationToken cancellationToken)
        {
            _themes[theme.Id] = theme;
            return Task.CompletedTask;
        }

        public Task<ThemeManifest?> GetByIdAsync(Guid themeId, CancellationToken cancellationToken)
        {
            _themes.TryGetValue(themeId, out var theme);
            return Task.FromResult(theme);
        }

        public Task<IReadOnlyList<ThemeManifest>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ThemeManifest>>(_themes.Values.ToList());
        }

        public Task<ThemeManifest?> GetActiveAsync(DateTimeOffset whenUtc, CancellationToken cancellationToken)
        {
            var active = _themes.Values
                .OrderByDescending(x => x.Priority)
                .ThenByDescending(x => x.UpdatedUtc)
                .FirstOrDefault(x => x.IsApplicableAt(whenUtc));

            return Task.FromResult(active);
        }

        public Task DeactivateAllAsync(CancellationToken cancellationToken)
        {
            var updated = _themes.Values
                .Select(x => x.Deactivate(DateTimeOffset.UtcNow))
                .ToList();

            _themes.Clear();
            foreach (var theme in updated)
            {
                _themes[theme.Id] = theme;
            }

            return Task.CompletedTask;
        }
    }
}
