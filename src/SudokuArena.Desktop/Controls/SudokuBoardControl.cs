using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SudokuArena.Desktop.Animations;
using SudokuArena.Desktop.Theming;

namespace SudokuArena.Desktop.Controls;

public sealed class SudokuBoardControl : FrameworkElement
{
    private const double CompletionDefaultMaxOpacity = 0.58d;
    private const double CompletionDefaultRingDelayMs = 85d;
    private const double CompletionDefaultPulseDurationMs = 320d;
    private const double VictoryMaxOpacity = 0.68d;
    private const double VictoryRingDelayMs = 65d;
    private const double VictoryPulseDurationMs = 420d;

    private readonly DispatcherTimer _completionTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(16)
    };

    private DateTimeOffset _completionStartedUtc;
    private IReadOnlyDictionary<int, int> _completionDistances = new Dictionary<int, int>();
    private int _completionMaxDistance;
    private double _completionPulseOpacity = CompletionDefaultMaxOpacity;
    private double _completionRingDelayMs = CompletionDefaultRingDelayMs;
    private double _completionPulseDurationMs = CompletionDefaultPulseDurationMs;

    public static readonly DependencyProperty CellsProperty = DependencyProperty.Register(
        nameof(Cells),
        typeof(IReadOnlyList<int?>),
        typeof(SudokuBoardControl),
        new FrameworkPropertyMetadata(Array.Empty<int?>(), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty GivenCellsProperty = DependencyProperty.Register(
        nameof(GivenCells),
        typeof(IReadOnlyList<bool>),
        typeof(SudokuBoardControl),
        new FrameworkPropertyMetadata(Array.Empty<bool>(), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
        nameof(SelectedIndex),
        typeof(int),
        typeof(SudokuBoardControl),
        new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SelectedNumberProperty = DependencyProperty.Register(
        nameof(SelectedNumber),
        typeof(int),
        typeof(SudokuBoardControl),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty InvalidCellsProperty = DependencyProperty.Register(
        nameof(InvalidCells),
        typeof(IReadOnlyList<bool>),
        typeof(SudokuBoardControl),
        new FrameworkPropertyMetadata(Array.Empty<bool>(), FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<int?> Cells
    {
        get => (IReadOnlyList<int?>)GetValue(CellsProperty);
        set => SetValue(CellsProperty, value);
    }

    public IReadOnlyList<bool> GivenCells
    {
        get => (IReadOnlyList<bool>)GetValue(GivenCellsProperty);
        set => SetValue(GivenCellsProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public int SelectedNumber
    {
        get => (int)GetValue(SelectedNumberProperty);
        set => SetValue(SelectedNumberProperty, value);
    }

    public IReadOnlyList<bool> InvalidCells
    {
        get => (IReadOnlyList<bool>)GetValue(InvalidCellsProperty);
        set => SetValue(InvalidCellsProperty, value);
    }

    public event EventHandler<int>? CellSelected;

    public event EventHandler<CellEditedEventArgs>? CellEdited;

    public event EventHandler<int>? NumberQuickSelected;

    public SudokuBoardControl()
    {
        _completionTimer.Tick += OnCompletionTick;
        Unloaded += (_, _) => _completionTimer.Stop();
    }

    public void StartCompletionAnimation(int originIndex, bool rowCompleted, bool columnCompleted, bool boxCompleted)
    {
        var distances = CompletionAnimationPlanner.BuildDistances(originIndex, rowCompleted, columnCompleted, boxCompleted);
        if (distances.Count == 0)
        {
            return;
        }

        _completionPulseOpacity = CompletionDefaultMaxOpacity;
        _completionRingDelayMs = CompletionDefaultRingDelayMs;
        _completionPulseDurationMs = CompletionDefaultPulseDurationMs;
        _completionDistances = distances;
        _completionMaxDistance = distances.Values.Max();
        _completionStartedUtc = DateTimeOffset.UtcNow;
        if (!_completionTimer.IsEnabled)
        {
            _completionTimer.Start();
        }

        InvalidateVisual();
    }

    public void StartVictoryAnimation(int originIndex)
    {
        var safeOriginIndex = originIndex is >= 0 and < 81 ? originIndex : 40;
        var distances = CompletionAnimationPlanner.BuildBoardWaveDistances(safeOriginIndex);
        if (distances.Count == 0)
        {
            return;
        }

        _completionPulseOpacity = VictoryMaxOpacity;
        _completionRingDelayMs = VictoryRingDelayMs;
        _completionPulseDurationMs = VictoryPulseDurationMs;
        _completionDistances = distances;
        _completionMaxDistance = distances.Values.Max();
        _completionStartedUtc = DateTimeOffset.UtcNow;
        if (!_completionTimer.IsEnabled)
        {
            _completionTimer.Start();
        }

        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var size = Math.Min(ActualWidth, ActualHeight);
        var cell = size / 9d;
        var boardRect = new Rect(0, 0, size, size);
        var palette = BoardThemePalette.FromResources(this);

        dc.DrawRectangle(palette.BoardBackgroundBrush, null, boardRect);

        if (SelectedIndex is >= 0 and < 81)
        {
            var selectedRow = SelectedIndex / 9;
            var selectedCol = SelectedIndex % 9;
            var boxStartRow = (selectedRow / 3) * 3;
            var boxStartCol = (selectedCol / 3) * 3;
            var rowHighlight = new Rect(0, selectedRow * cell, size, cell);
            var colHighlight = new Rect(selectedCol * cell, 0, cell, size);
            var boxHighlight = new Rect(boxStartCol * cell, boxStartRow * cell, 3 * cell, 3 * cell);
            dc.DrawRectangle(palette.RelatedGroupBrush, null, rowHighlight);
            dc.DrawRectangle(palette.RelatedGroupBrush, null, colHighlight);
            dc.DrawRectangle(palette.RelatedGroupBrush, null, boxHighlight);
        }

        DrawSelectedNumberHighlights(dc, cell, palette);
        DrawCompletionPulse(dc, cell, palette);

        if (SelectedIndex is >= 0 and < 81)
        {
            var selectedRow = SelectedIndex / 9;
            var selectedCol = SelectedIndex % 9;
            var highlight = new Rect(selectedCol * cell, selectedRow * cell, cell, cell);
            dc.DrawRectangle(palette.ActiveCellBrush, null, highlight);
        }

        DrawGrid(dc, size, cell, palette);
        DrawValues(dc, cell, palette);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var side = Math.Min(availableSize.Width, availableSize.Height);
        if (double.IsInfinity(side))
        {
            side = 540;
        }

        return new Size(side, side);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        Focus();

        if (e.ChangedButton == MouseButton.Right)
        {
            SelectedIndex = -1;
            CellSelected?.Invoke(this, -1);
            e.Handled = true;
            return;
        }

        var size = Math.Min(ActualWidth, ActualHeight);
        var cellSize = size / 9d;
        var point = e.GetPosition(this);
        if (point.X < 0 || point.Y < 0 || point.X >= size || point.Y >= size)
        {
            return;
        }

        var col = (int)(point.X / cellSize);
        var row = (int)(point.Y / cellSize);
        var clickedIndex = (row * 9) + col;
        SelectedIndex = clickedIndex;

        CellSelected?.Invoke(this, clickedIndex);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is >= Key.D1 and <= Key.D9)
        {
            var value = e.Key - Key.D0;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                SelectedIndex = -1;
                CellSelected?.Invoke(this, -1);
                NumberQuickSelected?.Invoke(this, value);
            }
            else if (SelectedIndex is < 0 or > 80)
            {
                NumberQuickSelected?.Invoke(this, value);
            }
            else
            {
                CellEdited?.Invoke(this, new CellEditedEventArgs(SelectedIndex, value));
            }
            e.Handled = true;
            return;
        }

        if (e.Key is >= Key.NumPad1 and <= Key.NumPad9)
        {
            var value = e.Key - Key.NumPad0;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                SelectedIndex = -1;
                CellSelected?.Invoke(this, -1);
                NumberQuickSelected?.Invoke(this, value);
            }
            else if (SelectedIndex is < 0 or > 80)
            {
                NumberQuickSelected?.Invoke(this, value);
            }
            else
            {
                CellEdited?.Invoke(this, new CellEditedEventArgs(SelectedIndex, value));
            }
            e.Handled = true;
            return;
        }

        if (SelectedIndex is < 0 or > 80)
        {
            return;
        }

        if (e.Key is Key.Back or Key.Delete or Key.D0 or Key.NumPad0)
        {
            CellEdited?.Invoke(this, new CellEditedEventArgs(SelectedIndex, null));
            e.Handled = true;
        }
    }

    private static void DrawGrid(DrawingContext dc, double size, double cell, BoardThemePalette palette)
    {
        for (var i = 0; i <= 9; i++)
        {
            var isBold = i % 3 == 0;
            var pen = new Pen(
                isBold ? palette.GridMajorBrush : palette.GridMinorBrush,
                isBold ? 2.2 : 1);

            var offset = i * cell;
            dc.DrawLine(pen, new Point(offset, 0), new Point(offset, size));
            dc.DrawLine(pen, new Point(0, offset), new Point(size, offset));
        }
    }

    private void DrawValues(DrawingContext dc, double cell, BoardThemePalette palette)
    {
        var cells = Cells;
        var givens = GivenCells;
        var invalidCells = InvalidCells;
        if (cells.Count < 81 || givens.Count < 81 || invalidCells.Count < 81)
        {
            return;
        }

        for (var i = 0; i < 81; i++)
        {
            var value = cells[i];
            if (value is null)
            {
                continue;
            }

            var row = i / 9;
            var col = i % 9;
            var text = new FormattedText(
                value.Value.ToString(CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                cell * 0.48,
                GetValueBrush(i, givens, invalidCells, palette),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            var x = (col * cell) + ((cell - text.Width) / 2);
            var y = (row * cell) + ((cell - text.Height) / 2);
            dc.DrawText(text, new Point(x, y));
        }
    }

    private void DrawSelectedNumberHighlights(DrawingContext dc, double cell, BoardThemePalette palette)
    {
        if (SelectedNumber is < 1 or > 9)
        {
            return;
        }

        var cells = Cells;
        if (cells.Count < 81)
        {
            return;
        }

        for (var i = 0; i < 81; i++)
        {
            if (cells[i] != SelectedNumber)
            {
                continue;
            }

            var row = i / 9;
            var col = i % 9;
            dc.DrawRectangle(
                palette.MatchingDigitBrush,
                null,
                new Rect(col * cell, row * cell, cell, cell));
        }
    }

    private static Brush GetValueBrush(
        int index,
        IReadOnlyList<bool> givens,
        IReadOnlyList<bool> invalidCells,
        BoardThemePalette palette)
    {
        if (invalidCells[index])
        {
            return palette.ConflictTextBrush;
        }

        return givens[index] ? palette.GivenTextBrush : palette.UserTextBrush;
    }

    private void DrawCompletionPulse(DrawingContext dc, double cell, BoardThemePalette palette)
    {
        if (_completionDistances.Count == 0)
        {
            return;
        }

        var elapsedMs = (DateTimeOffset.UtcNow - _completionStartedUtc).TotalMilliseconds;
        if (elapsedMs < 0)
        {
            return;
        }

        var baseColor = palette.CompletionPulseBrush is SolidColorBrush solidBrush
            ? solidBrush.Color
            : Color.FromRgb(74, 120, 194);

        foreach (var entry in _completionDistances)
        {
            var progress = (elapsedMs - (entry.Value * _completionRingDelayMs)) / _completionPulseDurationMs;
            if (progress is < 0 or > 1)
            {
                continue;
            }

            var intensity = progress < 0.5
                ? progress * 2
                : (1 - progress) * 2;
            if (intensity <= 0)
            {
                continue;
            }

            var alpha = (byte)Math.Clamp((int)(255 * _completionPulseOpacity * intensity), 0, 255);
            var pulseBrush = new SolidColorBrush(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));
            var row = entry.Key / 9;
            var col = entry.Key % 9;
            dc.DrawRectangle(pulseBrush, null, new Rect(col * cell, row * cell, cell, cell));
        }
    }

    private void OnCompletionTick(object? sender, EventArgs e)
    {
        if (_completionDistances.Count == 0)
        {
            _completionTimer.Stop();
            return;
        }

        var totalDurationMs = (_completionMaxDistance * _completionRingDelayMs) + _completionPulseDurationMs;
        var elapsedMs = (DateTimeOffset.UtcNow - _completionStartedUtc).TotalMilliseconds;
        if (elapsedMs > totalDurationMs)
        {
            _completionDistances = new Dictionary<int, int>();
            _completionMaxDistance = 0;
            _completionTimer.Stop();
        }

        InvalidateVisual();
    }
}

public sealed record CellEditedEventArgs(int Index, int? Value);



