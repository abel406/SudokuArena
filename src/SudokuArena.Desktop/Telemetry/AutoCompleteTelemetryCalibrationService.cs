using System.IO;
using System.Text.Json;
using SudokuArena.Application.AutoComplete;
using SudokuArena.Application.Puzzles;

namespace SudokuArena.Desktop.Telemetry;

public sealed class AutoCompleteTelemetryCalibrationService
{
    private const string CalibrationSchemaVersion = "sudokuarena.autocomplete_calibration.v1";
    private readonly string _eventsPath;
    private readonly string _calibrationPath;
    private readonly int _minSamplesPerTier;

    public AutoCompleteTelemetryCalibrationService(
        string? eventsPath = null,
        string? calibrationPath = null,
        int minSamplesPerTier = 12)
    {
        _eventsPath = string.IsNullOrWhiteSpace(eventsPath)
            ? AutoCompleteTelemetryPaths.GetDefaultEventsPath()
            : eventsPath;
        _calibrationPath = string.IsNullOrWhiteSpace(calibrationPath)
            ? AutoCompleteTelemetryPaths.GetDefaultCalibrationPath()
            : calibrationPath;
        _minSamplesPerTier = Math.Max(5, minSamplesPerTier);
    }

    public bool TryRebuildCalibration()
    {
        if (!File.Exists(_eventsPath))
        {
            return false;
        }

        List<AutoCompleteDiagnosticEvent> events;
        try
        {
            events = File.ReadLines(_eventsPath)
                .Select(TryDeserializeEvent)
                .Where(e => e is not null)
                .Select(e => e!)
                .ToList();
        }
        catch
        {
            return false;
        }

        if (events.Count == 0)
        {
            return false;
        }

        var tiers = new Dictionary<string, CalibrationEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in events.GroupBy(MapTier))
        {
            var tier = group.Key;
            var starts = group.Count(e => IsType(e, "start"));
            if (starts < _minSamplesPerTier)
            {
                continue;
            }

            var cancels = group.Count(e => IsType(e, "cancel"));
            var cancelRate = starts == 0 ? 0d : cancels / (double)starts;
            var completionRatios = group
                .Where(e => (IsType(e, "cancel") || IsType(e, "finish")) && e.QueueTotal > 0)
                .Select(e => Math.Clamp(e.QueueCompleted / (double)e.QueueTotal, 0d, 1d))
                .ToList();
            var avgCompletion = completionRatios.Count == 0 ? 0d : completionRatios.Average();

            var defaults = AutoCompletePolicyDefaults.GetCalibration(tier);
            var adjusted = ApplyHeuristic(defaults, cancelRate, avgCompletion);

            tiers[tier.ToString()] = new CalibrationEntry(
                adjusted.MinRemainingToTrigger,
                adjusted.MaxRemainingToTrigger,
                adjusted.TickIntervalMilliseconds,
                starts,
                cancels,
                Math.Round(avgCompletion, 4));
        }

        if (tiers.Count == 0)
        {
            return false;
        }

        try
        {
            var document = new CalibrationDocument(
                CalibrationSchemaVersion,
                DateTimeOffset.UtcNow,
                _minSamplesPerTier,
                tiers);
            var directory = Path.GetDirectoryName(_calibrationPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(
                document,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_calibrationPath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static AutoCompleteDiagnosticEvent? TryDeserializeEvent(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AutoCompleteDiagnosticEvent>(line);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsType(AutoCompleteDiagnosticEvent e, string value)
    {
        return string.Equals(e.EventType, value, StringComparison.OrdinalIgnoreCase);
    }

    private static DifficultyTier MapTier(AutoCompleteDiagnosticEvent e)
    {
        return Enum.TryParse<DifficultyTier>(e.DifficultyTier, ignoreCase: true, out var tier)
            ? tier
            : DifficultyTier.Medium;
    }

    private static AutoCompleteTierCalibration ApplyHeuristic(
        AutoCompleteTierCalibration defaults,
        double cancelRate,
        double avgCompletion)
    {
        var min = defaults.MinRemainingToTrigger;
        var max = defaults.MaxRemainingToTrigger;
        var interval = defaults.TickIntervalMilliseconds;

        if (cancelRate >= 0.45)
        {
            min++;
            max--;
            interval += 25;
        }
        else if (cancelRate <= 0.15 && avgCompletion >= 0.85)
        {
            min--;
            max++;
            interval -= 25;
        }

        if (avgCompletion <= 0.50)
        {
            interval += 25;
        }
        else if (avgCompletion >= 0.95)
        {
            interval -= 25;
        }

        min = Math.Clamp(min, 3, 12);
        max = Math.Clamp(max, min, 14);
        interval = Math.Clamp(interval, 160, 360);
        return new AutoCompleteTierCalibration(min, max, interval);
    }

    private sealed record CalibrationDocument(
        string SchemaVersion,
        DateTimeOffset GeneratedAtUtc,
        int MinSamplesPerTier,
        IReadOnlyDictionary<string, CalibrationEntry> Tiers);

    private sealed record CalibrationEntry(
        int MinRemainingToTrigger,
        int MaxRemainingToTrigger,
        int TickIntervalMilliseconds,
        int Starts,
        int Cancellations,
        double AvgCompletionRatio);
}
