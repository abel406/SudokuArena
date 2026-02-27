using System.Windows;
using SudokuArena.Desktop.ViewModels;

namespace SudokuArena.Desktop.Dialogs;

public partial class VictoryWindow : Window
{
    public VictoryWindow(VictorySummary summary)
    {
        InitializeComponent();
        DataContext = new VictoryWindowViewModel(summary);
    }

    private void OnContinueClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}

public sealed class VictoryWindowViewModel
{
    public VictoryWindowViewModel(VictorySummary summary)
    {
        ScoreText = $"Puntaje final: {summary.FinalScore}";
        DifficultyText = $"Nivel: {summary.DifficultyLabel}";
        TimeText = $"Tiempo: {summary.ElapsedTimeText}";
        ErrorText = $"Errores: {summary.ErrorCount}/3";
        PerfectText = summary.IsPerfect ? "Partida perfecta: SI" : "Partida perfecta: NO";
        ScoreModelText = $"Modelo activo: {summary.ScoreVersion}";
        MoveFillText = $"Fill-time (jugadas): +{summary.MoveFillScore}";
        ClearText = $"Cierres de unidad: +{summary.ClearScore}";
        NumberUseUpText = $"Numero agotado: +{summary.NumberUseUpScore}";
        TimeBonusText = $"Bonus tiempo final: +{summary.TimeBonusScore}";
        ErrorBonusText = $"Bonus margen errores: +{summary.ErrorBonusScore}";
        PerfectBonusText = $"Bonus perfecto: +{summary.PerfectBonusScore}";
        PenaltyText = $"Penalizaciones: {summary.PenaltyScore}";
        RawScoresText = $"Old/New: {summary.OldScore}/{summary.NewScore}";
    }

    public string ScoreText { get; }

    public string DifficultyText { get; }

    public string TimeText { get; }

    public string ErrorText { get; }

    public string PerfectText { get; }

    public string ScoreModelText { get; }

    public string MoveFillText { get; }

    public string ClearText { get; }

    public string NumberUseUpText { get; }

    public string TimeBonusText { get; }

    public string ErrorBonusText { get; }

    public string PerfectBonusText { get; }

    public string PenaltyText { get; }

    public string RawScoresText { get; }
}
