using System.IO;

namespace StarRuptureSaveFixer.Utils;

public static class SavePathHelper
{
    private const string STEAM_USERDATA_PATH = @"C:\Program Files (x86)\Steam\userdata";
    private const string SAVE_GAME_SUBPATH = @"1631270\remote\Saved\SaveGames";

    /// <summary>
    /// Finds the best default path for the save file dialog.
    /// Priority:
    /// 1. First valid Steam profile path containing the save game directory
    /// 2. Steam userdata root if it exists
    /// 3. null (system default)
    /// </summary>
    public static string? GetDefaultSavePath()
    {
        // Check if Steam userdata directory exists
        if (!Directory.Exists(STEAM_USERDATA_PATH))
        {
            return null; // Use system default
        }

        try
        {
            // Get all subdirectories (Steam profile IDs)
            var profileDirectories = Directory.GetDirectories(STEAM_USERDATA_PATH);

            // Search each profile for the Star Rupture save game path
            foreach (var profileDir in profileDirectories)
            {
                string savePath = Path.Combine(profileDir, SAVE_GAME_SUBPATH);

                if (Directory.Exists(savePath))
                {
                    return savePath;
                }
            }

            // No profile contained the save game path, return userdata root
            return STEAM_USERDATA_PATH;
        }
        catch
        {
            // If any error occurs during directory scanning, fall back to userdata root
            return STEAM_USERDATA_PATH;
        }
    }
}
