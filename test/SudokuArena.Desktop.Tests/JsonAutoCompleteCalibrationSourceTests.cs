using System.Text.Json;
using SudokuArena.Application.Puzzles;
using SudokuArena.Desktop.Telemetry;

namespace SudokuArena.Desktop.Tests;

public sealed class JsonAutoCompleteCalibrationSourceTests
{
    [Fact]
    public void GetCalibration_ShouldReadTierCalibration_FromJsonFile()
    {
        var path = BuildTempFilePath("autocomplete-calibration.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var document = new
        {
            SchemaVersion = "sudokuarena.autocomplete_calibration.v1",
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            MinSamplesPerTier = 12,
            Tiers = new Dictionary<string, object>
            {
                ["Medium"] = new
                {
                    MinRemainingToTrigger = 6,
                    MaxRemainingToTrigger = 10,
                    TickIntervalMilliseconds = 280,
                    Starts = 30,
                    Cancellations = 8,
                    AvgCompletionRatio = 0.74
                }
            }
        };
        File.WriteAllText(path, JsonSerializer.Serialize(document));
        var source = new JsonAutoCompleteCalibrationSource(path);

        var calibration = source.GetCalibration(DifficultyTier.Medium);

        Assert.NotNull(calibration);
        Assert.Equal(6, calibration!.MinRemainingToTrigger);
        Assert.Equal(10, calibration.MaxRemainingToTrigger);
        Assert.Equal(280, calibration.TickIntervalMilliseconds);
    }

    [Fact]
    public void GetCalibration_ShouldReturnNull_WhenFileDoesNotExist()
    {
        var source = new JsonAutoCompleteCalibrationSource(BuildTempFilePath("missing.json"));

        var calibration = source.GetCalibration(DifficultyTier.Medium);

        Assert.Null(calibration);
    }

    private static string BuildTempFilePath(string fileName)
    {
        return Path.Combine(
            Path.GetTempPath(),
            "SudokuArenaTests",
            Guid.NewGuid().ToString("N"),
            fileName);
    }
}
