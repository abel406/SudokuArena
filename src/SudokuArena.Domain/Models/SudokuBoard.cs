namespace SudokuArena.Domain.Models;

public sealed class SudokuBoard
{
    public const int BoardSize = 81;

    private readonly int?[] _cells;
    private readonly bool[] _givens;

    private SudokuBoard(int?[] cells, bool[] givens)
    {
        _cells = cells;
        _givens = givens;
    }

    public IReadOnlyList<int?> Cells => _cells;

    public IReadOnlyList<bool> Givens => _givens;

    public bool IsComplete => _cells.All(v => v is >= 1 and <= 9) && HasNoConflicts();

    public static SudokuBoard CreateFromString(string puzzle)
    {
        if (string.IsNullOrWhiteSpace(puzzle) || puzzle.Length != BoardSize)
        {
            throw new ArgumentException("Puzzle must contain exactly 81 characters.", nameof(puzzle));
        }

        var cells = new int?[BoardSize];
        var givens = new bool[BoardSize];

        for (var i = 0; i < BoardSize; i++)
        {
            var ch = puzzle[i];
            if (ch is '.' or '0')
            {
                cells[i] = null;
                givens[i] = false;
                continue;
            }

            if (!char.IsDigit(ch) || ch == '0')
            {
                throw new ArgumentException("Puzzle can only contain digits 1-9, 0 or '.'.", nameof(puzzle));
            }

            var value = ch - '0';
            cells[i] = value;
            givens[i] = true;
        }

        var board = new SudokuBoard(cells, givens);
        if (!board.HasNoConflicts())
        {
            throw new ArgumentException("Initial puzzle is invalid (contains conflicts).", nameof(puzzle));
        }

        return board;
    }

    public bool TrySetCell(int index, int? value, out string? reason)
    {
        reason = null;
        if (index < 0 || index >= BoardSize)
        {
            reason = "Cell index out of range.";
            return false;
        }

        if (_givens[index])
        {
            reason = "Given cells cannot be changed.";
            return false;
        }

        if (value is < 1 or > 9)
        {
            reason = "Value must be null or between 1 and 9.";
            return false;
        }

        if (value is null)
        {
            _cells[index] = null;
            return true;
        }

        if (!IsMoveValid(index, value.Value))
        {
            reason = "Move conflicts with row, column or sub-grid.";
            return false;
        }

        _cells[index] = value;
        return true;
    }

    public bool IsMoveValid(int index, int value)
    {
        if (index < 0 || index >= BoardSize || value is < 1 or > 9)
        {
            return false;
        }

        var row = index / 9;
        var col = index % 9;

        for (var i = 0; i < 9; i++)
        {
            var rowIndex = (row * 9) + i;
            if (rowIndex != index && _cells[rowIndex] == value)
            {
                return false;
            }

            var colIndex = (i * 9) + col;
            if (colIndex != index && _cells[colIndex] == value)
            {
                return false;
            }
        }

        var boxStartRow = (row / 3) * 3;
        var boxStartCol = (col / 3) * 3;
        for (var r = boxStartRow; r < boxStartRow + 3; r++)
        {
            for (var c = boxStartCol; c < boxStartCol + 3; c++)
            {
                var boxIndex = (r * 9) + c;
                if (boxIndex != index && _cells[boxIndex] == value)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public string ToStateString()
    {
        return string.Concat(_cells.Select(v => v?.ToString() ?? "."));
    }

    private bool HasNoConflicts()
    {
        for (var i = 0; i < BoardSize; i++)
        {
            if (_cells[i] is int value && !IsMoveValid(i, value))
            {
                return false;
            }
        }

        return true;
    }
}
