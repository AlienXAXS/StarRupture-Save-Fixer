namespace StarRuptureSaveFixer.Utils;

/// <summary>
/// Provides colored console logging with aligned tags
/// </summary>
public static class ConsoleLogger
{
    private const int TagWidth = 9; // Width for "[WARNING]" which is the longest tag

    public static void Info(string message)
    {
        WriteLog("INFO", ConsoleColor.Cyan, message);
    }

    public static void Success(string message)
    {
        WriteLog("SUCCESS", ConsoleColor.Green, message);
    }

    public static void Warning(string message)
    {
        WriteLog("WARNING", ConsoleColor.Yellow, message);
    }

    public static void Error(string message)
    {
        WriteLog("ERROR", ConsoleColor.Red, message);
    }

    public static void Progress(string message)
    {
        WriteLog("PROGRESS", ConsoleColor.Magenta, message);
    }

    private static void WriteLog(string tag, ConsoleColor color, string message)
    {
        // Save original color
        ConsoleColor originalColor = Console.ForegroundColor;

        // Write colored tag with padding
        Console.ForegroundColor = color;
        Console.Write($"[{tag.PadRight(TagWidth - 2)}]");

        // Reset to original color and write message
        Console.ForegroundColor = originalColor;
        Console.WriteLine($" {message}");
    }

    /// <summary>
    /// Writes a plain message without any tag
    /// </summary>
    public static void Plain(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Writes a header with a separator line
    /// </summary>
    public static void Header(params string[] lines)
    {
        if (lines == null || lines.Length == 0)
            return;

        // Find the longest line
        int maxLength = lines.Max(line => line?.Length ?? 0);

        // Write each line
        foreach (string line in lines)
        {
            Console.WriteLine(line);
        }

        // Write separator matching the longest line
        Console.WriteLine(new string('=', maxLength));
        Console.WriteLine();
    }
}
