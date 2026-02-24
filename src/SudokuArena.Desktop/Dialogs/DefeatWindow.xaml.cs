using System.Windows;

namespace SudokuArena.Desktop.Dialogs;

public partial class DefeatWindow : Window
{
    public DefeatWindow()
    {
        InitializeComponent();
    }

    public bool ContinuePlaying { get; private set; }

    private void OnContinueClicked(object sender, RoutedEventArgs e)
    {
        ContinuePlaying = true;
        DialogResult = true;
        Close();
    }

    private void OnFinishClicked(object sender, RoutedEventArgs e)
    {
        ContinuePlaying = false;
        DialogResult = false;
        Close();
    }
}
