using StarRuptureSaveFixer.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace StarRuptureSaveFixer;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch
        {
            // Ignore errors opening browser
        }
    }

    private void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenUpdatePage();
    }
}
