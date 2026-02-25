using SudokuArena.Application.Puzzles;

namespace SudokuArena.Desktop.Theming;

public interface IThemePreferenceStore
{
    ThemeMode? LoadThemeMode();

    void SaveThemeMode(ThemeMode mode);

    DifficultyTier? LoadDifficultyTier();

    void SaveDifficultyTier(DifficultyTier tier);
}
