using System.Text.Json;
using SudokuArena.Desktop.Telemetry;

namespace SudokuArena.Desktop.Tests;

public sealed class JsonLineAutoCompleteDiagnosticsSinkTests
{
    [Fact]
    public void Record_ShouldAppendJsonLine()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "SudokuArenaTests",
            Guid.NewGuid().ToString("N"),
            "autocomplete-events.ndjson");
        var sink = new JsonLineAutoCompleteDiagnosticsSink(path);
        var diagnosticEvent = new AutoCompleteDiagnosticEvent(
            DateTimeOffset.UtcNow,
            "start",
            "Medium",
            8,
            0,
            8,
            250);

        sink.Record(diagnosticEvent);

        Assert.True(File.Exists(path));
        var lines = File.ReadAllLines(path);
        Assert.Single(lines);
        var deserialized = JsonSerializer.Deserialize<AutoCompleteDiagnosticEvent>(lines[0]);
        Assert.NotNull(deserialized);
        Assert.Equal("start", deserialized!.EventType);
        Assert.Equal("Medium", deserialized.DifficultyTier);
        Assert.Equal(8, deserialized.QueueTotal);
    }
}
