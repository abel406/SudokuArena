using SudokuArena.Desktop.Theming;

namespace SudokuArena.Desktop.Tests;

public sealed class ThemeManagerTests
{
    [Fact]
    public void ResolveEffectiveMode_ShouldRespectExplicitLightMode()
    {
        var manager = new ThemeManager(new FakeDetector(ThemeMode.Dark));

        var effective = manager.ResolveEffectiveMode(ThemeMode.Light);

        Assert.Equal(ThemeMode.Light, effective);
    }

    [Fact]
    public void ResolveEffectiveMode_ShouldRespectExplicitDarkMode()
    {
        var manager = new ThemeManager(new FakeDetector(ThemeMode.Light));

        var effective = manager.ResolveEffectiveMode(ThemeMode.Dark);

        Assert.Equal(ThemeMode.Dark, effective);
    }

    [Fact]
    public void ResolveEffectiveMode_ShouldUseSystemDetector_WhenModeIsSystem()
    {
        var manager = new ThemeManager(new FakeDetector(ThemeMode.Dark));

        var effective = manager.ResolveEffectiveMode(ThemeMode.System);

        Assert.Equal(ThemeMode.Dark, effective);
    }

    [Fact]
    public void ResolveEffectiveMode_ShouldFallbackToLight_WhenDetectorThrows()
    {
        var manager = new ThemeManager(new ThrowingDetector());

        var effective = manager.ResolveEffectiveMode(ThemeMode.System);

        Assert.Equal(ThemeMode.Light, effective);
    }

    private sealed class FakeDetector(ThemeMode mode) : ISystemThemeDetector
    {
        public ThemeMode DetectPreferredMode() => mode;
    }

    private sealed class ThrowingDetector : ISystemThemeDetector
    {
        public ThemeMode DetectPreferredMode()
        {
            throw new InvalidOperationException("Simulated detector failure.");
        }
    }
}
