using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SudokuArena.Desktop.Controls;

public sealed class SudokuBoardControl : FrameworkElement
{
    private static readonly Color SelectionColor = Color.FromArgb(68, 139, 176, 237);
    private static readonly Color CurrentCellColor = Color.FromArgb(96, 139, 176, 237);

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

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var size = Math.Min(ActualWidth, ActualHeight);
        var cell = size / 9d;
        var boardRect = new Rect(0, 0, size, size);
        var selectionBrush = new SolidColorBrush(SelectionColor);

        dc.DrawRectangle(Brushes.White, null, boardRect);

        DrawSelectedNumberHighlights(dc, cell);

        if (SelectedIndex is >= 0 and < 81)
        {
            var selectedRow = SelectedIndex / 9;
            var selectedCol = SelectedIndex % 9;
            var boxStartRow = (selectedRow / 3) * 3;
            var boxStartCol = (selectedCol / 3) * 3;
            var rowHighlight = new Rect(0, selectedRow * cell, size, cell);
            var colHighlight = new Rect(selectedCol * cell, 0, cell, size);
            var boxHighlight = new Rect(boxStartCol * cell, boxStartRow * cell, 3 * cell, 3 * cell);
            dc.DrawRectangle(selectionBrush, null, rowHighlight);
            dc.DrawRectangle(selectionBrush, null, colHighlight);
            dc.DrawRectangle(selectionBrush, null, boxHighlight);

            var highlight = new Rect(selectedCol * cell, selectedRow * cell, cell, cell);
            dc.DrawRectangle(new SolidColorBrush(CurrentCellColor), null, highlight);
        }

        DrawGrid(dc, size, cell);
        DrawValues(dc, cell);
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
        SelectedIndex = (row * 9) + col;
        CellSelected?.Invoke(this, SelectedIndex);
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

    private static void DrawGrid(DrawingContext dc, double size, double cell)
    {
        for (var i = 0; i <= 9; i++)
        {
            var isBold = i % 3 == 0;
            var pen = new Pen(
                new SolidColorBrush(isBold ? Color.FromRgb(117, 123, 135) : Color.FromRgb(186, 191, 201)),
                isBold ? 2.2 : 1);

            var offset = i * cell;
            dc.DrawLine(pen, new Point(offset, 0), new Point(offset, size));
            dc.DrawLine(pen, new Point(0, offset), new Point(size, offset));
        }
    }

    private void DrawValues(DrawingContext dc, double cell)
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
                new SolidColorBrush(GetValueColor(i, givens, invalidCells, value.Value)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            var x = (col * cell) + ((cell - text.Width) / 2);
            var y = (row * cell) + ((cell - text.Height) / 2);
            dc.DrawText(text, new Point(x, y));
        }
    }

    private void DrawSelectedNumberHighlights(DrawingContext dc, double cell)
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

        var targetGrid = -1;
        if (SelectedIndex is >= 0 and < 81)
        {
            var selectedRow = SelectedIndex / 9;
            var selectedCol = SelectedIndex % 9;
            targetGrid = ((selectedRow / 3) * 3) + (selectedCol / 3);
        }

        for (var i = 0; i < 81; i++)
        {
            if (cells[i] != SelectedNumber)
            {
                continue;
            }

            if (targetGrid >= 0)
            {
                var cellRow = i / 9;
                var cellCol = i % 9;
                var grid = ((cellRow / 3) * 3) + (cellCol / 3);
                if (grid != targetGrid)
                {
                    continue;
                }
            }

            var row = i / 9;
            var col = i % 9;
            dc.DrawRectangle(
                new SolidColorBrush(SelectionColor),
                null,
                new Rect(col * cell, row * cell, cell, cell));
        }
    }

    private Color GetValueColor(int index, IReadOnlyList<bool> givens, IReadOnlyList<bool> invalidCells, int value)
    {
        if (invalidCells[index])
        {
            return Color.FromRgb(201, 41, 41);
        }

        return Color.FromRgb(30, 30, 34);
    }
}

public sealed record CellEditedEventArgs(int Index, int? Value);



