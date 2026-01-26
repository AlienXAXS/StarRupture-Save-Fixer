using StarRuptureSaveFixer.Services;
using StarRuptureSaveFixer.Utils;
using System.Diagnostics;

namespace StarRuptureSaveFixer.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly UpdateChecker _updateChecker;
    private readonly SettingsService _settingsService;
    private string _statusMessage = "Ready";
    private bool _isUpdateAvailable;
    private string? _updateUrl;

    public MainViewModel()
    {
        _updateChecker = new UpdateChecker();
        _settingsService = new SettingsService();

        // Load saved custom path
        var settings = _settingsService.LoadSettings();
        var customPath = settings.CustomSavePath;

        SaveBrowser = new SaveBrowserViewModel(customPath);
        SessionManager = new SessionManagerViewModel(customPath);
        FtpSync = new FtpSyncViewModel(customPath);
        Settings = new SettingsViewModel(OnSettingsChanged);

        // Check for updates asynchronously
        _ = CheckForUpdatesAsync();
    }

    public SaveBrowserViewModel SaveBrowser { get; }
    public SessionManagerViewModel SessionManager { get; }
    public FtpSyncViewModel FtpSync { get; }
    public SettingsViewModel Settings { get; }

    private void OnSettingsChanged()
    {
        // Reload settings and update all ViewModels
        var settings = _settingsService.LoadSettings();
        var customPath = settings.CustomSavePath;

        SaveBrowser.UpdateCustomPath(customPath);
        SessionManager.UpdateCustomPath(customPath);
        FtpSync.UpdateCustomPath(customPath);

        RefreshAll();
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        set => SetProperty(ref _isUpdateAvailable, value);
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var updateInfo = await _updateChecker.CheckForUpdatesAsync();

            if (updateInfo != null && updateInfo.UpdateAvailable)
            {
                IsUpdateAvailable = true;
                _updateUrl = updateInfo.DownloadUrl;
                StatusMessage = $"Update available: v{updateInfo.LatestVersion}";
            }
            else if (updateInfo != null)
            {
                StatusMessage = $"Version {updateInfo.CurrentVersion}";
            }
        }
        catch
        {
            // Ignore update check errors
        }
    }

    public void OpenUpdatePage()
    {
        if (string.IsNullOrEmpty(_updateUrl))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _updateUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening browser
        }
    }

    public void RefreshAll()
    {
        SaveBrowser.Refresh();
        SessionManager.Refresh();
        FtpSync.Refresh();
    }
}
