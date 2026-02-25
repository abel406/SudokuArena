using System.Text.Json.Serialization;

namespace SudokuArena.Application.Puzzles;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DifficultyTier
{
    Beginner = 0,
    Easy = 1,
    Medium = 2,
    Hard = 3,
    Expert = 4
}
