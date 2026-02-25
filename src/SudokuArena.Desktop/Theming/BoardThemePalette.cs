using System.Windows;
using System.Windows.Media;

namespace SudokuArena.Desktop.Theming;

public sealed class BoardThemePalette
{
    public required Brush BoardBackgroundBrush { get; init; }
    public required Brush RelatedGroupBrush { get; init; }
    public required Brush ActiveCellBrush { get; init; }
    public required Brush MatchingDigitBrush { get; init; }
    public required Brush CompletionPulseBrush { get; init; }
    public required Brush GridMajorBrush { get; init; }
    public required Brush GridMinorBrush { get; init; }
    public required Brush ConflictTextBrush { get; init; }
    public required Brush GivenTextBrush { get; init; }
    public required Brush UserTextBrush { get; init; }

    public static BoardThemePalette FromResources(FrameworkElement element)
    {
        return new BoardThemePalette
        {
            BoardBackgroundBrush = GetBrush(element, ThemeResourceKeys.BoardBackgroundBrush, Colors.White),
            RelatedGroupBrush = GetBrush(element, ThemeResourceKeys.BoardRelatedGroupBrush, Color.FromArgb(86, 216, 220, 226)),
            ActiveCellBrush = GetBrush(element, ThemeResourceKeys.BoardActiveCellBrush, Color.FromArgb(96, 139, 176, 237)),
            MatchingDigitBrush = GetBrush(element, ThemeResourceKeys.BoardMatchingDigitBrush, Color.FromArgb(96, 139, 176, 237)),
            CompletionPulseBrush = GetBrush(element, ThemeResourceKeys.BoardCompletionPulseBrush, Color.FromArgb(160, 139, 176, 237)),
            GridMajorBrush = GetBrush(element, ThemeResourceKeys.BoardGridMajorBrush, Color.FromRgb(117, 123, 135)),
            GridMinorBrush = GetBrush(element, ThemeResourceKeys.BoardGridMinorBrush, Color.FromRgb(186, 191, 201)),
            ConflictTextBrush = GetBrush(element, ThemeResourceKeys.BoardConflictTextBrush, Color.FromRgb(201, 41, 41)),
            GivenTextBrush = GetBrush(element, ThemeResourceKeys.BoardGivenTextBrush, Color.FromRgb(30, 30, 34)),
            UserTextBrush = GetBrush(element, ThemeResourceKeys.BoardUserTextBrush, Color.FromRgb(30, 30, 34))
        };
    }

    private static Brush GetBrush(FrameworkElement element, string key, Color fallbackColor)
    {
        if (element.TryFindResource(key) is Brush brush)
        {
            return brush;
        }

        return new SolidColorBrush(fallbackColor);
    }
}
