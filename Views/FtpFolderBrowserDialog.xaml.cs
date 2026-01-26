using StarRuptureSaveFixer.Models;
using StarRuptureSaveFixer.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StarRuptureSaveFixer.Views;

public partial class FtpFolderBrowserDialog : Window
{
    private readonly FtpService _ftpService;
    private readonly FtpSettings _settings;
    private readonly string _password;
    private readonly string _initialPath;
    private string _currentPath = "/";
    private CancellationTokenSource? _cts;
    private bool _isLoading;

    public string SelectedPath { get; private set; } = "/";
    public ObservableCollection<FtpDirectoryItem> Items { get; } = new();

    public FtpFolderBrowserDialog(FtpSettings settings, string password, string initialPath = "/")
    {
        InitializeComponent();

        _ftpService = new FtpService();
        _settings = settings;
        _password = password;
        _initialPath = string.IsNullOrEmpty(initialPath) ? "/" : initialPath;

        FolderListBox.ItemsSource = Items;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Test connection first before loading directory
        if (!await TestConnectionAsync())
        {
            StatusTextBlock.Text = "Connection failed. Please check your FTP settings.";
            SelectButton.IsEnabled = false;

            // Close the dialog since connection failed
            DialogResult = false;
            Close();
            return;
        }

        await LoadDirectoryAsync(_initialPath);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        CancelCurrentOperation();
    }

    private async Task<bool> TestConnectionAsync()
    {
        CancelCurrentOperation();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        SetLoading(true, "Testing connection...");
        StatusTextBlock.Text = "Testing connection...";

        try
        {
            var (success, message) = await _ftpService.TestConnectionAsync(_settings, _password, token);

            if (token.IsCancellationRequested)
                return false;

            if (!success)
            {
                StatusTextBlock.Text = $"Connection failed: {message}";
                MessageBox.Show($"Failed to connect to FTP server:\n\n{message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            StatusTextBlock.Text = "Connection successful";
            return true;
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "Connection test cancelled";
            return false;
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Connection error: {ex.Message}";
            MessageBox.Show($"Failed to connect to FTP server:\n\n{ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void SetLoading(bool loading, string message = "Loading...")
    {
        _isLoading = loading;
        LoadingOverlay.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        LoadingText.Text = message;
        UpButton.IsEnabled = !loading;
        RefreshButton.IsEnabled = !loading;
        FolderListBox.IsEnabled = !loading;
        CancelOperationButton.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;

        if (loading)
        {
            SelectButton.IsEnabled = false;
        }
    }

    private void CancelCurrentOperation()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task LoadDirectoryAsync(string path)
    {
        CancelCurrentOperation();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        SetLoading(true, "Connecting to server...");
        StatusTextBlock.Text = "Connecting...";
        Items.Clear();

        try
        {
            var items = await _ftpService.ListRemoteDirectoryAsync(path, _settings, _password, token);

            if (token.IsCancellationRequested)
                return;

            // Update UI on main thread
            foreach (var item in items.Where(i => i.IsDirectory))
            {
                Items.Add(item);
            }

            _currentPath = path;
            CurrentPathTextBox.Text = path;
            SelectButton.IsEnabled = true;
            StatusTextBlock.Text = $"Found {Items.Count} folder(s)";
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "Operation cancelled";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void CancelOperationButton_Click(object sender, RoutedEventArgs e)
    {
        CancelCurrentOperation();
        SetLoading(false);
        StatusTextBlock.Text = "Operation cancelled";
    }

    private async void UpButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
            return;

        if (_currentPath == "/" || string.IsNullOrEmpty(_currentPath))
            return;

        var parentPath = GetParentPath(_currentPath);
        await LoadDirectoryAsync(parentPath);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
            return;

        await LoadDirectoryAsync(_currentPath);
    }

    private async void FolderListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_isLoading)
            return;

        if (FolderListBox.SelectedItem is FtpDirectoryItem item && item.IsDirectory)
        {
            await LoadDirectoryAsync(item.FullPath);
        }
    }

    private void FolderListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Selection changed - could update status if needed
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        // If a folder is selected in the list, assume the user meant to pick that folder
        if (FolderListBox.SelectedItem is FtpDirectoryItem selectedItem && selectedItem.IsDirectory)
        {
            SelectedPath = selectedItem.FullPath;
        }
        else
        {
            // Otherwise use the folder currently being browsed
            SelectedPath = _currentPath;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CancelCurrentOperation();
        DialogResult = false;
        Close();
    }

    private static string GetParentPath(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
            return "/";

        path = path.TrimEnd('/');
        var lastSlash = path.LastIndexOf('/');

        if (lastSlash <= 0)
            return "/";

        return path.Substring(0, lastSlash);
    }
}
