namespace SudokuArena.Desktop.Theming;

public interface IThemePreferenceStore
{
    ThemeMode? LoadThemeMode();

    void SaveThemeMode(ThemeMode mode);
}
