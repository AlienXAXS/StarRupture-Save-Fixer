namespace StarRuptureSaveFixer.Utils;

/// <summary>
/// Parses command-line arguments
/// </summary>
public class ArgumentParser
{
    private readonly Dictionary<string, string> _arguments = new();
    private readonly HashSet<string> _flags = new();

    /// <summary>
    /// Parses command-line arguments in the format: -key value or -flag
    /// </summary>
    public ArgumentParser(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("-"))
            {
                string key = args[i].TrimStart('-').ToLower();

                // Check if next item exists and is not another flag
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    // This is a key-value pair
                    string value = args[i + 1];
                    _arguments[key] = value;
                    i++; // Skip the next item since it's the value
                }
                else
                {
                    // This is a standalone flag
                    _flags.Add(key);
                }
            }
        }
    }

    /// <summary>
    /// Gets the value for a given argument key
    /// </summary>
    public string? GetValue(string key)
    {
        return _arguments.TryGetValue(key.ToLower(), out var value) ? value : null;
    }

    /// <summary>
    /// Checks if an argument exists (either as a key-value pair or as a flag)
    /// </summary>
    public bool HasArgument(string key)
    {
        string lowerKey = key.ToLower();
        return _arguments.ContainsKey(lowerKey) || _flags.Contains(lowerKey);
    }

    /// <summary>
    /// Checks if a flag exists
    /// </summary>
    public bool HasFlag(string flag)
    {
        return _flags.Contains(flag.ToLower());
    }

    /// <summary>
    /// Gets all flags that have been set
    /// </summary>
    public IEnumerable<string> GetFlags()
    {
        return _flags;
    }
}
