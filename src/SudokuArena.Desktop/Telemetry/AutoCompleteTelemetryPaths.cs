using System.IO;

namespace SudokuArena.Desktop.Telemetry;

public static class AutoCompleteTelemetryPaths
{
    public static string GetDefaultEventsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SudokuArena",
            "autocomplete-events.ndjson");
    }

    public static string GetDefaultCalibrationPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SudokuArena",
            "autocomplete-calibration.json");
    }
}
