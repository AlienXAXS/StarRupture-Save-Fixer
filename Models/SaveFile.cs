namespace StarRuptureSaveFixer.Models;

/// <summary>
/// Represents a Star Rupture save file with its JSON content
/// </summary>
public class SaveFile
{
    /// <summary>
    /// The JSON content of the save file
    /// </summary>
    public string JsonContent { get; set; } = string.Empty;

    /// <summary>
    /// The original file path
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}
