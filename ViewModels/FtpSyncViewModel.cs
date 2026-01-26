using StarRuptureSaveFixer.Models;
using StarRuptureSaveFixer.Services;
using StarRuptureSaveFixer.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace StarRuptureSaveFixer.ViewModels;

public class FtpSyncViewModel : ViewModelBase
{
    private readonly SessionManager _sessionManager;
    private readonly FtpService _ftpService;
    private readonly SettingsService _settingsService;

    // Shared FTP settings
    private string _host = "";
    private int _port = 21;
    private string _username = "";
    private string _password = "";
    private string _remotePath = "/";
    private bool _useFtps;
    private bool _passiveMode = true;

    // Connection test fields
    private bool _isTesting;
    private string _connectionStatus = "Ready to connect";
    private Brush _connectionStatusColor = Brushes.Gray;
    private CancellationTokenSource? _testCts;

    // Upload-specific fields
    private SaveSession? _selectedSession;
    private SaveFileInfo? _selectedFile;
    private int _uploadProgress;
    private string _uploadStatus = "";
    private bool _isUploading;
    private bool _remoteSessionSelected;
    private CancellationTokenSource? _uploadCts;
    private string _uploadFileInfo = "No file selected";
    private Brush _uploadFileInfoColor = Brushes.Gray;

    // Download-specific fields
    private bool _isDownloading;
    private int _downloadProgress;
    private string _downloadStatus = "";
    private bool _remoteHasSave;
    private CancellationTokenSource? _downloadCts;

    private const string REMOTE_SAVEGAMES_PATH = "/StarRupture/Saved/SaveGames";

    public FtpSyncViewModel(string? customPath = null)
    {
        _sessionManager = new SessionManager();
        _sessionManager.CustomSavePath = customPath;
        _ftpService = new FtpService();
        _settingsService = new SettingsService();

        Sessions = new ObservableCollection<SaveSession>();
        AvailableFiles = new ObservableCollection<SaveFileInfo>();

        // Shared commands
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync, () => CanTestConnection);
        CancelTestCommand = new RelayCommand(CancelTest, () => IsTesting);
        BrowseRemoteCommand = new RelayCommand(async () => await BrowseRemoteAsync(), () => CanBrowseRemote);
        SaveSettingsCommand = new RelayCommand(SaveSettings);
        RefreshCommand = new RelayCommand(Refresh);

        // Upload commands
        UploadCommand = new AsyncRelayCommand(UploadAsync, () => CanUpload);
        CancelUploadCommand = new RelayCommand(CancelUpload, () => IsUploading);
        SelectUploadFileCommand = new RelayCommand(SelectUploadFile);

        // Download commands
        DownloadCommand = new AsyncRelayCommand(DownloadAsync, () => CanDownload);
        CancelDownloadCommand = new RelayCommand(CancelDownload, () => IsDownloading);

        LoadSettings();
        Refresh();
    }

    public void UpdateCustomPath(string? customPath)
    {
        _sessionManager.CustomSavePath = customPath;
    }

    public ObservableCollection<SaveSession> Sessions { get; }
    public ObservableCollection<SaveFileInfo> AvailableFiles { get; }

    #region Shared FTP Settings

    public string Host
    {
        get => _host;
        set
        {
            if (SetProperty(ref _host, value))
            {
                OnPropertyChanged(nameof(CanTestConnection));
                OnPropertyChanged(nameof(CanBrowseRemote));
                OnPropertyChanged(nameof(CanUpload));
                OnPropertyChanged(nameof(CanDownload));
            }
        }
    }

    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string RemotePath
    {
        get => _remotePath;
        set
        {
            if (SetProperty(ref _remotePath, value))
            {
                RemoteHasSave = false; // reset when path changes
                OnPropertyChanged(nameof(CanDownload));
            }
        }
    }

    public bool UseFtps
    {
        get => _useFtps;
        set => SetProperty(ref _useFtps, value);
    }

    public bool PassiveMode
    {
        get => _passiveMode;
        set => SetProperty(ref _passiveMode, value);
    }

    #endregion

    #region Connection Test Properties

    public bool IsTesting
    {
        get => _isTesting;
        private set
        {
            if (SetProperty(ref _isTesting, value))
            {
                OnPropertyChanged(nameof(CanTestConnection));
                OnPropertyChanged(nameof(CanBrowseRemote));
                OnPropertyChanged(nameof(CanUpload));
                OnPropertyChanged(nameof(CanDownload));
            }
        }
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => SetProperty(ref _connectionStatus, value);
    }

    public Brush ConnectionStatusColor
    {
        get => _connectionStatusColor;
        private set => SetProperty(ref _connectionStatusColor, value);
    }

    #endregion

    #region Upload Properties

    public SaveSession? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (SetProperty(ref _selectedSession, value))
            {
                RefreshFileList();
            }
        }
    }

    public SaveFileInfo? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (SetProperty(ref _selectedFile, value))
            {
                OnPropertyChanged(nameof(CanUpload));
                OnPropertyChanged(nameof(SelectedFileInfo));
            }
        }
    }

    public string SelectedFileInfo => SelectedFile != null
        ? $"Selected: {SelectedFile.FileName} ({SelectedFile.FileSizeDisplay}) - Will be uploaded as AutoSave0.sav"
        : "Select a save file to upload";

    public int UploadProgress
    {
        get => _uploadProgress;
        set => SetProperty(ref _uploadProgress, value);
    }

    public string UploadStatus
    {
        get => _uploadStatus;
        set => SetProperty(ref _uploadStatus, value);
    }

    public bool IsUploading
    {
        get => _isUploading;
        set
        {
            if (SetProperty(ref _isUploading, value))
            {
                OnPropertyChanged(nameof(CanTestConnection));
                OnPropertyChanged(nameof(CanUpload));
                OnPropertyChanged(nameof(CanBrowseRemote));
            }
        }
    }

    public bool RemoteSessionSelected
    {
        get => _remoteSessionSelected;
        private set
        {
            if (SetProperty(ref _remoteSessionSelected, value))
            {
                OnPropertyChanged(nameof(CanUpload));
                OnPropertyChanged(nameof(CanDownload));
                UpdateRemoteSessionStatus();
            }
        }
    }

    public string UploadFileInfo
    {
        get => _uploadFileInfo;
        private set => SetProperty(ref _uploadFileInfo, value);
    }

    public Brush UploadFileInfoColor
    {
        get => _uploadFileInfoColor;
        private set => SetProperty(ref _uploadFileInfoColor, value);
    }

    #endregion

    #region Download Properties

    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            if (SetProperty(ref _isDownloading, value))
            {
                OnPropertyChanged(nameof(CanBrowseRemote));
                OnPropertyChanged(nameof(CanDownload));
                OnPropertyChanged(nameof(CanTestConnection));
            }
        }
    }

    public int DownloadProgress
    {
        get => _downloadProgress;
        private set => SetProperty(ref _downloadProgress, value);
    }

    public string DownloadStatus
    {
        get => _downloadStatus;
        private set => SetProperty(ref _downloadStatus, value);
    }

    public bool RemoteHasSave
    {
        get => _remoteHasSave;
        private set
        {
            if (SetProperty(ref _remoteHasSave, value))
            {
                OnPropertyChanged(nameof(CanDownload));
                UpdateRemoteSessionStatus();
            }
        }
    }

    private string _remoteSessionStatus = "No remote session selected";
    public string RemoteSessionStatus
    {
        get => _remoteSessionStatus;
        private set => SetProperty(ref _remoteSessionStatus, value);
    }

    private Brush _remoteSessionStatusColor = Brushes.Gray;
    public Brush RemoteSessionStatusColor
    {
        get => _remoteSessionStatusColor;
        private set => SetProperty(ref _remoteSessionStatusColor, value);
    }

    #endregion

    #region Computed Properties

    public bool CanTestConnection => !IsUploading && !IsDownloading && !IsTesting && !string.IsNullOrWhiteSpace(Host);
    public bool CanUpload => !IsUploading && !IsDownloading && !IsTesting && !string.IsNullOrWhiteSpace(Host) && SelectedFile != null && RemoteSessionSelected;
    public bool CanBrowseRemote => !IsUploading && !IsDownloading && !IsTesting && !string.IsNullOrWhiteSpace(Host);
    public bool CanDownload => !IsUploading && !IsDownloading && !IsTesting && !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(RemotePath) && RemoteHasSave;

    #endregion

    #region Commands

    public ICommand TestConnectionCommand { get; }
    public ICommand CancelTestCommand { get; }
    public ICommand BrowseRemoteCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand UploadCommand { get; }
    public ICommand CancelUploadCommand { get; }
    public ICommand DownloadCommand { get; }
    public ICommand CancelDownloadCommand { get; }
    public ICommand SelectUploadFileCommand { get; }

    #endregion

    #region Public Methods

    public void Refresh()
    {
        var previousSelection = SelectedSession?.FullPath;
        Sessions.Clear();

        foreach (var session in _sessionManager.GetAllSessions())
        {
            Sessions.Add(session);
        }

        if (previousSelection != null)
            SelectedSession = Sessions.FirstOrDefault(s => s.FullPath == previousSelection);
        else if (Sessions.Count > 0)
            SelectedSession = Sessions[0];
    }

    #endregion

    #region Private Methods

    private void RefreshFileList()
    {
        AvailableFiles.Clear();
        SelectedFile = null;

        if (SelectedSession == null)
            return;

        foreach (var file in SelectedSession.SaveFiles.Where(f => !f.IsBackup))
        {
            AvailableFiles.Add(file);
        }
    }

    private void UpdateRemoteSessionStatus()
    {
        if (!RemoteSessionSelected)
        {
            RemoteSessionStatus = "No remote session selected";
            RemoteSessionStatusColor = Brushes.Gray;
        }
        else if (RemoteHasSave)
        {
            RemoteSessionStatus = "[READY] Remote session ready for download";
            RemoteSessionStatusColor = Brushes.Green;
        }
        else
        {
            RemoteSessionStatus = "Remote session selected (checking for save files...)";
            RemoteSessionStatusColor = Brushes.Orange;
        }
    }

    private void CancelTest()
    {
        _testCts?.Cancel();
        ConnectionStatus = "Connection test cancelled";
        ConnectionStatusColor = Brushes.Orange;
    }

    private void CancelUpload()
    {
        _uploadCts?.Cancel();
        UploadStatus = "Cancelling...";
    }

    private void CancelDownload()
    {
        _downloadCts?.Cancel();
        DownloadStatus = "Cancelling...";
    }

    private void SelectUploadFile()
    {
        // Get all available sessions
        var sessions = _sessionManager.GetAllSessions();
        if (sessions.Count == 0)
        {
            UploadFileInfo = "No local sessions found";
            UploadFileInfoColor = Brushes.Red;
            SelectedFile = null;
            return;
        }

        // Show the select local save dialog
        SaveFileInfo? selectedFile = null;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            UploadFileInfo = "UI dispatcher not available";
            UploadFileInfoColor = Brushes.Red;
            return;
        }

        dispatcher.Invoke(() =>
        {
            var dialog = new SelectLocalSaveDialog(sessions)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                selectedFile = dialog.SelectedFile;
            }
        });

        if (selectedFile != null)
        {
            SelectedFile = selectedFile;
            UploadFileInfo = $"[SELECTED] {selectedFile.FileName} from {selectedFile.SessionName} ({selectedFile.FileSizeDisplay})";
            UploadFileInfoColor = Brushes.Green;
        }
        else
        {
            UploadFileInfo = "No file selected";
            UploadFileInfoColor = Brushes.Gray;
        }
    }

    private async Task TestConnectionAsync()
    {
        _testCts?.Cancel();
        _testCts = new CancellationTokenSource();
        var token = _testCts.Token;

        IsTesting = true;
        ConnectionStatus = "Testing connection...";
        ConnectionStatusColor = Brushes.Blue;

        try
        {
            var settings = CreateFtpSettings();
            var (success, message) = await _ftpService.TestConnectionAsync(settings, Password, token).ConfigureAwait(false);

            if (!token.IsCancellationRequested)
            {
                ConnectionStatus = message;
                ConnectionStatusColor = success ? Brushes.Green : Brushes.Red;
            }
        }
        catch (OperationCanceledException)
        {
            ConnectionStatus = "Connection test cancelled";
            ConnectionStatusColor = Brushes.Orange;
        }
        finally
        {
            IsTesting = false;
        }
    }

    private async Task BrowseRemoteAsync()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            ConnectionStatus = "Please enter a host first";
            ConnectionStatusColor = Brushes.Red;
            return;
        }

        var settings = CreateFtpSettings();

        // Try detecting the StarRupture savegames path on the server
        string initialPath = "/";
        try
        {
            var exists = await _ftpService.RemotePathExistsAsync(REMOTE_SAVEGAMES_PATH, settings, Password).ConfigureAwait(false);
            if (exists)
            {
                initialPath = REMOTE_SAVEGAMES_PATH;
            }
        }
        catch
        {
            // ignore and fall back to root
        }

        // Show folder browser dialog starting at the detected path on the UI thread
        string? selectedPath = null;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            ConnectionStatus = "Unable to open folder browser: UI dispatcher not available";
            ConnectionStatusColor = Brushes.Red;
            return;
        }

        try
        {
            dispatcher.Invoke(() =>
                  {
                      var dialog = new FtpFolderBrowserDialog(settings, Password, initialPath)
                      {
                          Owner = Application.Current.MainWindow
                      };

                      if (dialog.ShowDialog() == true)
                      {
                          selectedPath = dialog.SelectedPath;
                      }
                  });
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Failed to show folder browser: {ex.Message}";
            ConnectionStatusColor = Brushes.Red;
            return;
        }

        if (!string.IsNullOrEmpty(selectedPath))
        {
            RemotePath = selectedPath;

            // Check if it's a valid session for upload
            if (RemotePath.StartsWith(REMOTE_SAVEGAMES_PATH) && RemotePath != REMOTE_SAVEGAMES_PATH)
            {
                RemoteSessionSelected = true;
                UploadStatus = $"Selected remote session: {RemotePath}";
            }
            else
            {
                RemoteSessionSelected = false;
                UploadStatus = "Please select a session folder inside /StarRupture/Saved/SaveGames";
            }

            // Check for download validity
            DownloadStatus = "Checking remote folder for save files...";
            try
            {
                var listing = await _ftpService.ListRemoteDirectoryAsync(RemotePath, settings, Password).ConfigureAwait(false);
                bool hasSav = listing.Any(i => string.Equals(i.Name, "AutoSave0.sav", StringComparison.OrdinalIgnoreCase));
                bool hasMet = listing.Any(i => string.Equals(i.Name, "AutoSave0.met", StringComparison.OrdinalIgnoreCase));

                if (hasSav && hasMet)
                {
                    RemoteHasSave = true;
                    DownloadStatus = $"Remote session valid: {RemotePath}";
                }
                else
                {
                    RemoteHasSave = false;
                    DownloadStatus = "Remote path does not contain AutoSave0.sav and AutoSave0.met.";
                }
            }
            catch (Exception ex)
            {
                RemoteHasSave = false;
                DownloadStatus = $"Error checking remote path: {ex.Message}";
            }
        }
    }

    private async Task UploadAsync()
    {
        if (SelectedFile == null)
        {
            UploadStatus = "No file selected for upload.";
            return;
        }

        if (!RemoteSessionSelected)
        {
            UploadStatus = "Please select a remote session folder before uploading.";
            return;
        }

        _uploadCts?.Cancel();
        _uploadCts = new CancellationTokenSource();
        var token = _uploadCts.Token;

        IsUploading = true;
        UploadProgress = 0;
        UploadStatus = "Starting upload...";

        try
        {
            var settings = CreateFtpSettings();
            var progress = new Progress<(int Percent, string Status)>(p =>
                   {
                       if (!token.IsCancellationRequested)
                       {
                           UploadProgress = p.Percent;
                           UploadStatus = p.Status;
                       }
                   });

            // Always upload as AutoSave0.sav
            var success = await _ftpService.UploadFileAsAsync(
                    SelectedFile.FullPath,
              RemotePath,
                  "AutoSave0.sav",
                      settings,
               Password,
          progress,
                          token).ConfigureAwait(false);

            if (!token.IsCancellationRequested)
            {
                if (success)
                {
                    UploadStatus = "Successfully uploaded as AutoSave0.sav!";

                    // Update download status since we now have save files
                    RemoteHasSave = true;
                }
                else
                {
                    UploadStatus = "Upload failed.";
                }
            }
        }
        catch (OperationCanceledException)
        {
            UploadStatus = "Upload cancelled.";
            UploadProgress = 0;
        }
        catch (Exception ex)
        {
            UploadStatus = $"Upload failed: {ex.Message}";
            UploadProgress = 0;
        }
        finally
        {
            IsUploading = false;
        }
    }

    private string CombineRemotePathLocal(string basePath, string fileName)
    {
        if (string.IsNullOrEmpty(basePath) || basePath == "/")
            return "/" + fileName;

        basePath = basePath.TrimEnd('/');
        return basePath + "/" + fileName;
    }

    private async Task DownloadAsync()
    {
        _downloadCts?.Cancel();
        _downloadCts = new CancellationTokenSource();
        var token = _downloadCts.Token;

        if (string.IsNullOrWhiteSpace(RemotePath))
        {
            DownloadStatus = "No remote path selected.";
            return;
        }

        if (!RemoteHasSave)
        {
            DownloadStatus = "Remote path does not contain required save files.";
            return;
        }

        // Require existing local sessions
        var sessions = _sessionManager.GetAllSessions();
        if (sessions.Count == 0)
        {
            DownloadStatus = "No local sessions found. Cannot download.";
            return;
        }

        // Ask user to pick a session and file to overwrite
        SaveFileInfo? targetFile = null;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            DownloadStatus = "UI dispatcher not available.";
            return;
        }

        dispatcher.Invoke(() =>
   {
       var dialog = new SelectLocalSaveDialog(sessions)
       {
           Owner = Application.Current.MainWindow
       };

       if (dialog.ShowDialog() == true)
       {
           targetFile = dialog.SelectedFile;
       }
   });

        if (targetFile == null)
        {
            DownloadStatus = "Download cancelled or no target selected.";
            return;
        }

        // Confirm overwrite
        var confirm = dispatcher.Invoke(() => MessageBox.Show(
          $"Are you sure you want to overwrite '{targetFile.FileName}' in session '{targetFile.SessionName}' with the remote AutoSave0.sav?",
        "Confirm Overwrite",
  MessageBoxButton.YesNo,
            MessageBoxImage.Warning));

        if (confirm != MessageBoxResult.Yes)
        {
            DownloadStatus = "Download cancelled by user.";
            return;
        }

        // Proceed to download remote AutoSave0.sav and overwrite targetFile.FullPath
        var remoteAutoSave = CombineRemotePathLocal(RemotePath, "AutoSave0.sav");

        try
        {
            IsDownloading = true;
            DownloadProgress = 0;
            DownloadStatus = "Downloading AutoSave0.sav...";

            var settings = CreateFtpSettings();
            var progress = new Progress<(int Percent, string Status)>(p =>
                      {
                          if (!token.IsCancellationRequested)
                          {
                              DownloadProgress = p.Percent;
                              DownloadStatus = p.Status;
                          }
                      });

            var success = await _ftpService.DownloadFileAsync(remoteAutoSave, targetFile.FullPath, settings, Password, progress, token).ConfigureAwait(false);

            if (!token.IsCancellationRequested)
            {
                if (success)
                {
                    // Refresh main sessions list
                    var disp = Application.Current?.Dispatcher;
                    if (disp != null)
                    {
                        disp.Invoke(() =>
                     {
                         if (Application.Current.MainWindow?.DataContext is MainViewModel main)
                         {
                             main.RefreshAll();
                         }
                     });
                    }

                    DownloadStatus = $"Successfully overwrote {targetFile.FileName}";
                    DownloadProgress = 100;
                }
                else
                {
                    DownloadStatus = "Download failed.";
                }
            }
        }
        catch (OperationCanceledException)
        {
            DownloadStatus = "Download cancelled.";
            DownloadProgress = 0;
        }
        catch (Exception ex)
        {
            DownloadStatus = $"Download failed: {ex.Message}";
            DownloadProgress = 0;
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private void SaveSettings()
    {
        var appSettings = _settingsService.LoadSettings();

        appSettings.FtpSettings = CreateFtpSettings();
        appSettings.FtpSettings.EncryptedPassword = _settingsService.EncryptPassword(Password);

        _settingsService.SaveSettings(appSettings);
        ConnectionStatus = "Settings saved successfully!";
        ConnectionStatusColor = Brushes.Green;
    }

    private void LoadSettings()
    {
        var appSettings = _settingsService.LoadSettings();
        var ftpSettings = appSettings.FtpSettings;

        Host = ftpSettings.Host;
        Port = ftpSettings.Port;
        Username = ftpSettings.Username;
        Password = _settingsService.DecryptPassword(ftpSettings.EncryptedPassword);
        // Always start with root - user must select session each time
        RemotePath = "/";
        UseFtps = ftpSettings.UseFtps;
        PassiveMode = ftpSettings.PassiveMode;
    }

    private FtpSettings CreateFtpSettings()
    {
        return new FtpSettings
        {
            Host = Host,
            Port = Port,
            Username = Username,
            UseFtps = UseFtps,
            PassiveMode = PassiveMode
        };
    }

    #endregion
}
