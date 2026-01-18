using StarRuptureSaveFixer.Models;

namespace StarRuptureSaveFixer.Fixers;

/// <summary>
/// Interface for save file fixers
/// </summary>
public interface IFixer
{
    /// <summary>
    /// The name of the fix
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Applies the fix to the save file
    /// </summary>
    /// <param name="saveFile">The save file to fix</param>
    /// <returns>True if changes were made, false otherwise</returns>
    bool ApplyFix(SaveFile saveFile);
}
