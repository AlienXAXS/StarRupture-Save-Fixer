using StarRuptureSaveFixer.Fixers;
using StarRuptureSaveFixer.Models;
using StarRuptureSaveFixer.Services;
using StarRuptureSaveFixer.Utils;

namespace StarRuptureSaveFixer;

class Program
{
    static void Main(string[] args)
    {
        ConsoleLogger.Header("Star Rupture Save File Fixer", "   By AlienX", "", "Visit StarRupture-Utilities.com for more");

        try
        {
            // Parse command-line arguments
            var parser = new ArgumentParser(args);

            // Show help if no arguments provided
            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            // Check for required -file argument
            string? filePath = parser.GetValue("file");
            if (string.IsNullOrEmpty(filePath))
            {
                ConsoleLogger.Error("-file parameter is required.");
                ConsoleLogger.Plain("");
                ShowUsage();
                return;
            }

            // Collect all requested fixes
            List<IFixer> fixersToApply = new();

            bool hasFixDrones = parser.HasFlag("fixdrones");
            bool hasRemoveDrones = parser.HasFlag("removedrones");

            // Validate that both flags are not used together
            if (hasFixDrones && hasRemoveDrones)
            {
                ConsoleLogger.Error("-fixdrones and -removedrones cannot be used together.");
                ConsoleLogger.Plain("");
                ShowUsage();
                return;
            }

            if (hasFixDrones)
            {
                fixersToApply.Add(new DroneFixer());
            }

            if (hasRemoveDrones)
            {
                fixersToApply.Add(new DroneRemover());
            }

            // Check if any fixes were requested
            if (fixersToApply.Count == 0)
            {
                ConsoleLogger.Error("No fixes specified. Please provide at least one fix parameter.");
                ConsoleLogger.Plain("");
                ShowUsage();
                return;
            }

            // Initialize the save file service
            var saveFileService = new SaveFileService();

            // Load the save file
            ConsoleLogger.Info($"Loading save file: {filePath}");
            SaveFile saveFile = saveFileService.LoadSaveFile(filePath);
            ConsoleLogger.Success("Save file loaded successfully!");

            // Display JSON content info
            ConsoleLogger.Info($"JSON Size: {saveFile.JsonContent.Length:N0} bytes");

            // Apply all requested fixes
            ConsoleLogger.Info($"Applying {fixersToApply.Count} fix(es)...");
            bool anyChanges = false;

            foreach (var fixer in fixersToApply)
            {
                bool changed = fixer.ApplyFix(saveFile);
                anyChanges = anyChanges || changed;
            }

            // Save the modified file with _fixed suffix
            if (anyChanges)
            {
                string outputPath = GetFixedFilePath(filePath);
                ConsoleLogger.Info($"Saving fixed save file to: {outputPath}");
                saveFileService.SaveSaveFile(saveFile, outputPath);
                ConsoleLogger.Success("Save file saved successfully!");
                ConsoleLogger.Info($"Original file preserved at: {filePath}");
            }
            else
            {
                ConsoleLogger.Warning("No changes were made to the save file.");
            }

            ConsoleLogger.Info("Thanks for using this tool, press Enter to exit");
            Console.ReadLine();
        }
        catch (FileNotFoundException ex)
        {
            ConsoleLogger.Error(ex.Message);

            ConsoleLogger.Info("Thanks for using this tool, press Enter to exit");
            Console.ReadLine();
        }
        catch (InvalidDataException ex)
        {
            ConsoleLogger.Error(ex.Message);

            ConsoleLogger.Info("Thanks for using this tool, press Enter to exit");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            ConsoleLogger.Error($"Unexpected error: {ex.Message}");
            ConsoleLogger.Error($"Stack trace: {ex.StackTrace}");

            ConsoleLogger.Info("Thanks for using this tool, press Enter to exit");
            Console.ReadLine();
        }
    }

    static void ShowUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  StarRuptureSaveFixer -file <path-to-save-file> [fix-options]");
        Console.WriteLine();
        Console.WriteLine("Required Parameters:");
        Console.WriteLine("  -file <path>      Path to the .sav file to fix");
        Console.WriteLine();
        Console.WriteLine("Fix Options (at least one required):");
        Console.WriteLine("  -fixdrones        Fix drone-related issues (removes drones with invalid targets)");
        Console.WriteLine("  -removedrones     Remove all drones from the save file");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  StarRuptureSaveFixer -file \"C:\\path\\to\\save.sav\" -fixdrones");
        Console.WriteLine("  StarRuptureSaveFixer -file \"C:\\path\\to\\save.sav\" -removedrones");
        Console.WriteLine();
        Console.WriteLine("Note: The original file will be preserved, and the fixed version");
        Console.WriteLine("      will be saved with '_fixed' appended to the filename.");

        ConsoleLogger.Info("Thanks for using this tool, press Enter to exit");
        Console.ReadLine();
    }

    static string GetFixedFilePath(string originalPath)
    {
        string directory = Path.GetDirectoryName(originalPath) ?? "";
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
        string extension = Path.GetExtension(originalPath);

        return Path.Combine(directory, $"{fileNameWithoutExtension}_fixed{extension}");
    }

    static string GetPreview(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }
}
