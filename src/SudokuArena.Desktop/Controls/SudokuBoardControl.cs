using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SudokuArena.Desktop.Controls;

public sealed class SudokuBoardControl : FrameworkElement
{
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

    public event EventHandler<int>? CellSelected;

    public event EventHandler<CellEditedEventArgs>? CellEdited;

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var size = Math.Min(ActualWidth, ActualHeight);
        var cell = size / 9d;
        var boardRect = new Rect(0, 0, size, size);

        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(255, 253, 248)), null, boardRect);

        if (SelectedIndex is >= 0 and < 81)
        {
            var selectedRow = SelectedIndex / 9;
            var selectedCol = SelectedIndex % 9;
            var highlight = new Rect(selectedCol * cell, selectedRow * cell, cell, cell);
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(90, 28, 110, 140)), null, highlight);
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
        if (SelectedIndex is < 0 or > 80)
        {
            return;
        }

        if (e.Key is >= Key.D1 and <= Key.D9)
        {
            var value = e.Key - Key.D0;
            CellEdited?.Invoke(this, new CellEditedEventArgs(SelectedIndex, value));
            e.Handled = true;
            return;
        }

        if (e.Key is >= Key.NumPad1 and <= Key.NumPad9)
        {
            var value = e.Key - Key.NumPad0;
            CellEdited?.Invoke(this, new CellEditedEventArgs(SelectedIndex, value));
            e.Handled = true;
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
                new SolidColorBrush(isBold ? Color.FromRgb(45, 49, 66) : Color.FromRgb(158, 163, 173)),
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
        if (cells.Count < 81 || givens.Count < 81)
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
                new SolidColorBrush(givens[i] ? Color.FromRgb(45, 49, 66) : Color.FromRgb(224, 122, 95)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            var x = (col * cell) + ((cell - text.Width) / 2);
            var y = (row * cell) + ((cell - text.Height) / 2);
            dc.DrawText(text, new Point(x, y));
        }
    }
}

public sealed record CellEditedEventArgs(int Index, int? Value);
