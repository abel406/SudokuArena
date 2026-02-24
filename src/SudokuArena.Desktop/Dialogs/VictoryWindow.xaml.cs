using System.Windows;

namespace SudokuArena.Desktop.Dialogs;

public partial class VictoryWindow : Window
{
    public VictoryWindow(int score, string difficulty)
    {
        InitializeComponent();
        DataContext = new VictoryWindowViewModel(score, difficulty);
    }

    private void OnContinueClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}

public sealed class VictoryWindowViewModel
{
    public VictoryWindowViewModel(int score, string difficulty)
    {
        ScoreText = $"Puntaje: {score}";
        DifficultyText = $"Nivel: {difficulty}";
    }

    public string ScoreText { get; }

    public string DifficultyText { get; }
}
