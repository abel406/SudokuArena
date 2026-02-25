namespace SudokuArena.Desktop.Animations;

public static class CompletionAnimationPlanner
{
    public static IReadOnlyDictionary<int, int> BuildBoardWaveDistances(int originIndex)
    {
        if (originIndex is < 0 or >= 81)
        {
            return new Dictionary<int, int>();
        }

        var originRow = originIndex / 9;
        var originCol = originIndex % 9;
        var result = new Dictionary<int, int>(capacity: 81);

        for (var index = 0; index < 81; index++)
        {
            var row = index / 9;
            var col = index % 9;
            var distance = Math.Max(Math.Abs(row - originRow), Math.Abs(col - originCol));
            result[index] = distance;
        }

        return result;
    }

    public static IReadOnlyDictionary<int, int> BuildDistances(
        int originIndex,
        bool rowCompleted,
        bool columnCompleted,
        bool boxCompleted)
    {
        if (originIndex is < 0 or >= 81 || (!rowCompleted && !columnCompleted && !boxCompleted))
        {
            return new Dictionary<int, int>();
        }

        var originRow = originIndex / 9;
        var originCol = originIndex % 9;
        var result = new Dictionary<int, int>(capacity: 81);

        for (var index = 0; index < 81; index++)
        {
            var row = index / 9;
            var col = index % 9;
            var distance = int.MaxValue;

            if (rowCompleted && row == originRow)
            {
                distance = Math.Min(distance, Math.Abs(col - originCol));
            }

            if (columnCompleted && col == originCol)
            {
                distance = Math.Min(distance, Math.Abs(row - originRow));
            }

            if (boxCompleted && IsInSameBox(originRow, originCol, row, col))
            {
                distance = Math.Min(distance, Math.Abs(row - originRow) + Math.Abs(col - originCol));
            }

            if (distance != int.MaxValue)
            {
                result[index] = distance;
            }
        }

        return result;
    }

    private static bool IsInSameBox(int originRow, int originCol, int row, int col)
    {
        return (originRow / 3) == (row / 3) && (originCol / 3) == (col / 3);
    }
}
