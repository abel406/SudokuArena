using System.IO;
using System.Text.Json;
using SudokuArena.Application.AutoComplete;
using SudokuArena.Application.Puzzles;

namespace SudokuArena.Desktop.Telemetry;

public sealed class JsonAutoCompleteCalibrationSource : IAutoCompleteCalibrationSource
{
    private readonly string _calibrationFilePath;
    private readonly object _sync = new();
    private Dictionary<DifficultyTier, AutoCompleteTierCalibration> _cache = [];
    private DateTime _lastReadUtc = DateTime.MinValue;

    public JsonAutoCompleteCalibrationSource(string? calibrationFilePath = null)
    {
        _calibrationFilePath = string.IsNullOrWhiteSpace(calibrationFilePath)
            ? AutoCompleteTelemetryPaths.GetDefaultCalibrationPath()
            : calibrationFilePath;
    }

    public AutoCompleteTierCalibration? GetCalibration(DifficultyTier difficultyTier)
    {
        EnsureLoaded();
        return _cache.TryGetValue(difficultyTier, out var calibration)
            ? calibration
            : null;
    }

    private void EnsureLoaded()
    {
        lock (_sync)
        {
            if (!File.Exists(_calibrationFilePath))
            {
                _cache = [];
                _lastReadUtc = DateTime.MinValue;
                return;
            }

            var fileLastWriteUtc = File.GetLastWriteTimeUtc(_calibrationFilePath);
            if (fileLastWriteUtc <= _lastReadUtc && _cache.Count > 0)
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(_calibrationFilePath);
                var document = JsonSerializer.Deserialize<CalibrationDocument>(json);
                if (document?.Tiers is null)
                {
                    _cache = [];
                    _lastReadUtc = fileLastWriteUtc;
                    return;
                }

                var next = new Dictionary<DifficultyTier, AutoCompleteTierCalibration>();
                foreach (var (key, value) in document.Tiers)
                {
                    if (!Enum.TryParse<DifficultyTier>(key, ignoreCase: true, out var tier))
                    {
                        continue;
                    }

                    if (value.MinRemainingToTrigger < 1 ||
                        value.MaxRemainingToTrigger < value.MinRemainingToTrigger ||
                        value.TickIntervalMilliseconds < 100)
                    {
                        continue;
                    }

                    next[tier] = new AutoCompleteTierCalibration(
                        value.MinRemainingToTrigger,
                        value.MaxRemainingToTrigger,
                        value.TickIntervalMilliseconds);
                }

                _cache = next;
                _lastReadUtc = fileLastWriteUtc;
            }
            catch
            {
                _cache = [];
                _lastReadUtc = fileLastWriteUtc;
            }
        }
    }

    private sealed record CalibrationDocument(
        string SchemaVersion,
        DateTimeOffset GeneratedAtUtc,
        IReadOnlyDictionary<string, CalibrationEntry> Tiers);

    private sealed record CalibrationEntry(
        int MinRemainingToTrigger,
        int MaxRemainingToTrigger,
        int TickIntervalMilliseconds,
        int Starts,
        int Cancellations,
        double AvgCompletionRatio);
}
