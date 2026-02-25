using System.Text.Json.Serialization;

namespace SudokuArena.Application.Puzzles;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PuzzleBoardKind
{
    Classic9x9 = 0,
    SixBySix = 1,
    SixteenBySixteen = 2
}
