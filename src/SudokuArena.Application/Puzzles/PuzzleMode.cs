using System.Text.Json.Serialization;

namespace SudokuArena.Application.Puzzles;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PuzzleMode
{
    Unknown = 0,
    Beginner = 1,
    Easy = 2,
    Medium = 3,
    Hard = 4,
    Expert = 5,
    Extreme = 6,
    Six = 7,
    Sixteen = 8
}
