using System.Net.Http;
using System.Text.Json;
using System.Reflection;

namespace StarRuptureSaveFixer.Services;

public class UpdateChecker
{
    private const string GITHUB_API_URL = "https://api.github.com/repos/AlienXAXS/StarRupture-Save-Fixer/releases/latest";
    private static readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "StarRupture-Save-Fixer" } }
    };

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(GITHUB_API_URL);
            var release = JsonDocument.Parse(response);
            var root = release.RootElement;

            string latestVersion = root.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "";
            string currentVersion = GetCurrentVersion();

            bool updateAvailable = IsNewerVersion(latestVersion, currentVersion);

            return new UpdateInfo
            {
                CurrentVersion = currentVersion,
                LatestVersion = latestVersion,
                UpdateAvailable = updateAvailable,
                DownloadUrl = root.GetProperty("html_url").GetString() ?? "",
                ReleaseNotes = root.GetProperty("body").GetString() ?? ""
            };
        }
        catch
        {
            return null; // Silently fail if update check fails
        }
    }

    private string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
    }

    private bool IsNewerVersion(string latest, string current)
    {
        try
        {
            var latestParts = latest.Split('.').Select(int.Parse).ToArray();
            var currentParts = current.Split('.').Select(int.Parse).ToArray();

            for (int i = 0; i < Math.Min(latestParts.Length, currentParts.Length); i++)
            {
                if (latestParts[i] > currentParts[i]) return true;
                if (latestParts[i] < currentParts[i]) return false;
            }

            return latestParts.Length > currentParts.Length;
        }
        catch
        {
            return false;
        }
    }
}

public class UpdateInfo
{
    public string CurrentVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public bool UpdateAvailable { get; set; }
    public string DownloadUrl { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
}
