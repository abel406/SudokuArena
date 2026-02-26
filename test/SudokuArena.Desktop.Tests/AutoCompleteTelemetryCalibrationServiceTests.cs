using System.Text.Json;
using SudokuArena.Application.Puzzles;
using SudokuArena.Desktop.Telemetry;

namespace SudokuArena.Desktop.Tests;

public sealed class AutoCompleteTelemetryCalibrationServiceTests
{
    [Fact]
    public void TryRebuildCalibration_ShouldGenerateCalibrationFile_WhenSamplesAreEnough()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "SudokuArenaTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var eventsPath = Path.Combine(directory, "autocomplete-events.ndjson");
        var calibrationPath = Path.Combine(directory, "autocomplete-calibration.json");

        var events = new List<AutoCompleteDiagnosticEvent>();
        for (var i = 0; i < 12; i++)
        {
            events.Add(new AutoCompleteDiagnosticEvent(
                DateTimeOffset.UtcNow.AddSeconds(i),
                "start",
                "Medium",
                8,
                0,
                10,
                250));
        }

        for (var i = 0; i < 6; i++)
        {
            events.Add(new AutoCompleteDiagnosticEvent(
                DateTimeOffset.UtcNow.AddSeconds(20 + i),
                "cancel",
                "Medium",
                7,
                2,
                10,
                250));
        }

        for (var i = 0; i < 2; i++)
        {
            events.Add(new AutoCompleteDiagnosticEvent(
                DateTimeOffset.UtcNow.AddSeconds(40 + i),
                "finish",
                "Medium",
                0,
                10,
                10,
                250));
        }

        File.WriteAllLines(eventsPath, events.Select(e => JsonSerializer.Serialize(e)));
        var service = new AutoCompleteTelemetryCalibrationService(eventsPath, calibrationPath, minSamplesPerTier: 10);

        var changed = service.TryRebuildCalibration();
        var source = new JsonAutoCompleteCalibrationSource(calibrationPath);
        var calibration = source.GetCalibration(DifficultyTier.Medium);

        Assert.True(changed);
        Assert.NotNull(calibration);
        Assert.Equal(6, calibration!.MinRemainingToTrigger);
        Assert.Equal(8, calibration.MaxRemainingToTrigger);
        Assert.Equal(300, calibration.TickIntervalMilliseconds);
    }

    [Fact]
    public void TryRebuildCalibration_ShouldReturnFalse_WhenNoEnoughSamples()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "SudokuArenaTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var eventsPath = Path.Combine(directory, "autocomplete-events.ndjson");
        var calibrationPath = Path.Combine(directory, "autocomplete-calibration.json");

        var events = new[]
        {
            new AutoCompleteDiagnosticEvent(DateTimeOffset.UtcNow, "start", "Hard", 8, 0, 10, 225),
            new AutoCompleteDiagnosticEvent(DateTimeOffset.UtcNow.AddSeconds(1), "cancel", "Hard", 8, 1, 10, 225)
        };
        File.WriteAllLines(eventsPath, events.Select(e => JsonSerializer.Serialize(e)));

        var service = new AutoCompleteTelemetryCalibrationService(eventsPath, calibrationPath, minSamplesPerTier: 10);
        var changed = service.TryRebuildCalibration();

        Assert.False(changed);
        Assert.False(File.Exists(calibrationPath));
    }
}
