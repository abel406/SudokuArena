using System.IO;
using System.Text.Json;

namespace SudokuArena.Desktop.Telemetry;

public sealed class JsonLineAutoCompleteDiagnosticsSink : IAutoCompleteDiagnosticsSink
{
    private readonly string _logFilePath;
    private readonly object _sync = new();

    public JsonLineAutoCompleteDiagnosticsSink(string? logFilePath = null)
    {
        _logFilePath = string.IsNullOrWhiteSpace(logFilePath)
            ? AutoCompleteTelemetryPaths.GetDefaultEventsPath()
            : logFilePath;
    }

    public void Record(AutoCompleteDiagnosticEvent diagnosticEvent)
    {
        try
        {
            lock (_sync)
            {
                var directory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var line = JsonSerializer.Serialize(diagnosticEvent);
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // No-op: telemetria nunca debe bloquear flujo de juego.
        }
    }
}
