using Microsoft.Win32;
using StarRuptureSaveFixer.Fixers;
using StarRuptureSaveFixer.Models;
using StarRuptureSaveFixer.Services;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Navigation;

namespace StarRuptureSaveFixer;

public partial class MainWindow : Window
{
    private readonly SaveFileService _saveFileService;

    public MainWindow()
    {
        InitializeComponent();
        _saveFileService = new SaveFileService();
        LogMessage("Welcome to Star Rupture Save File Fixer!", "Info");
        LogMessage("Select a save file and choose a fix option to get started.", "Info");
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Save Files (*.sav)|*.sav|All Files (*.*)|*.*",
            Title = "Select Star Rupture Save File"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            FilePathTextBox.Text = openFileDialog.FileName;
            LogMessage($"Selected file: {openFileDialog.FileName}", "Info");
        }
    }

    private async void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate file selection
        if (string.IsNullOrWhiteSpace(FilePathTextBox.Text))
        {
            LogMessage("Please select a save file first.", "Error");
            MessageBox.Show("Please select a save file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string filePath = FilePathTextBox.Text;

        // Validate file exists
        if (!File.Exists(filePath))
        {
            LogMessage($"File not found: {filePath}", "Error");
            MessageBox.Show("The selected file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Disable UI during processing
        ProcessButton.IsEnabled = false;
        BrowseButton.IsEnabled = false;
        FixDronesRadio.IsEnabled = false;
        RemoveDronesRadio.IsEnabled = false;

        // Clear previous log
        LogTextBox.Clear();
        LogMessage("Starting save file processing...", "Info");

        try
        {
            await Task.Run(() =>
            {
                // Load the save file
                Dispatcher.Invoke(() => LogMessage($"Loading save file: {filePath}", "Info"));
                SaveFile saveFile = _saveFileService.LoadSaveFile(filePath);
                Dispatcher.Invoke(() =>
                {
                    LogMessage("Save file loaded successfully!", "Success");
                    LogMessage($"JSON Size: {saveFile.JsonContent.Length:N0} bytes", "Info");
                });

                // Determine which fixer to use
                IFixer fixer;
                bool isFixDrones = false;
                Dispatcher.Invoke(() => isFixDrones = FixDronesRadio.IsChecked == true);

                if (isFixDrones)
                {
                    fixer = new DroneFixer();
                    Dispatcher.Invoke(() => LogMessage("Selected fix: Fix Drones (Remove invalid targets)", "Info"));
                }
                else
                {
                    fixer = new DroneRemover();
                    Dispatcher.Invoke(() => LogMessage("Selected fix: Remove All Drones", "Info"));
                }

                // Apply the fix
                Dispatcher.Invoke(() => LogMessage("Applying fix...", "Info"));
                bool changed = fixer.ApplyFix(saveFile);

                // Save the modified file
                if (changed)
                {
                    string backupPath = GetBackupFilePath(filePath);

                    // Rename original file to _original
                    Dispatcher.Invoke(() => LogMessage($"Backing up original file to: {backupPath}", "Info"));
                    File.Move(filePath, backupPath, overwrite: true);

                    // Save fixed file with original filename
                    Dispatcher.Invoke(() => LogMessage($"Saving fixed save file to: {filePath}", "Info"));
                    _saveFileService.SaveSaveFile(saveFile, filePath);

                    Dispatcher.Invoke(() =>
                    {
                        LogMessage("Save file saved successfully!", "Success");
                        LogMessage($"Original file backed up to: {backupPath}", "Info");
                    });

                    Dispatcher.Invoke(() => MessageBox.Show(
                        $"Save file fixed successfully!\n\nFixed file saved to:\n{filePath}\n\nOriginal backed up to:\n{backupPath}",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information));
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        LogMessage("No changes were made to the save file.", "Warning");
                        MessageBox.Show(
                            "No changes were needed for this save file.",
                            "No Changes",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                }
            });
        }
        catch (FileNotFoundException ex)
        {
            LogMessage(ex.Message, "Error");
            MessageBox.Show(ex.Message, "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (InvalidDataException ex)
        {
            LogMessage(ex.Message, "Error");
            MessageBox.Show(ex.Message, "Invalid Data", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            LogMessage($"Unexpected error: {ex.Message}", "Error");
            LogMessage($"Stack trace: {ex.StackTrace}", "Error");
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            // Re-enable UI after processing
            ProcessButton.IsEnabled = true;
            BrowseButton.IsEnabled = true;
            FixDronesRadio.IsEnabled = true;
            RemoveDronesRadio.IsEnabled = true;
        }
    }

    private void LogMessage(string message, string type = "Info")
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string prefix = type switch
        {
            "Error" => "[ERROR]",
            "Warning" => "[WARN]",
            "Success" => "[OK]",
            _ => "[INFO]"
        };

        LogTextBox.AppendText($"[{timestamp}] {prefix} {message}\n");
        LogTextBox.ScrollToEnd();
    }

    private string GetBackupFilePath(string originalPath)
    {
        string directory = Path.GetDirectoryName(originalPath) ?? "";
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
        string extension = Path.GetExtension(originalPath);

        return Path.Combine(directory, $"{fileNameWithoutExtension}_original{extension}");
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
        catch (Exception ex)
        {
            LogMessage($"Failed to open URL: {ex.Message}", "Error");
        }
    }
}
