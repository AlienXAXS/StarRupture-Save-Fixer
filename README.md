# Star Rupture Save Fixer

A command-line tool to fix corrupted save files for the game **Star Rupture**. This utility helps repair common save file issues, particularly those related to drones with invalid targets.

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows%20x64-blue)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Fix Drones** – Automatically detects and removes drones with invalid movement targets that can cause save corruption
- **Remove All Drones** – Completely removes all drone entities from your save file
- **Non-destructive** – Original save files are preserved; fixed versions are saved with `_fixed` suffix
- **Progress Feedback** – Visual progress indicators during processing

## Requirements

- Windows x64
- .NET 8.0 Runtime (or use the self-contained release)

## Installation

### Option 1: Download Release (Recommended)
Download the latest release from the [StarRupture Utilities Website](https://starrupture-utilities.com) 

### Option 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/YourUsername/StarRuptureSaveFixer.git
cd StarRuptureSaveFixer

# Build the project
dotnet build -c Release

# Or publish as a single self-contained executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Usage

```
StarRuptureSaveFixer -file <path-to-save-file> [fix-options]
```

### Parameters

| Parameter | Description |
|-----------|-------------|
| `-file <path>` | **(Required)** Path to the `.sav` file to fix |
| `-fixdrones` | Fix drone-related issues (removes drones with invalid targets) |
| `-removedrones` | Remove all drones from the save file |

> **Note:** `-fixdrones` and `-removedrones` cannot be used together.

### Examples

**Fix drones with invalid targets:**
```bash
StarRuptureSaveFixer -file "C:\path\to\your\save.sav" -fixdrones
```

**Remove all drones:**
```bash
StarRuptureSaveFixer -file "C:\path\to\your\save.sav" -removedrones
```

## How It Works

Star Rupture save files use a custom format:
1. **4-byte header** – Contains the uncompressed JSON size (little-endian)
2. **Compressed payload** – zlib/deflate compressed JSON data

The tool:
1. Reads and decompresses the save file
2. Parses the JSON structure to locate entity data
3. Identifies problematic entities (e.g., drones with invalid movement targets)
4. Removes or repairs the identified issues
5. Recompresses and saves the fixed file with a `_fixed` suffix

## Save File Location

Star Rupture save files are typically located at:
```
%LOCALAPPDATA%\StarRupture\Saves\
```

## Project Structure

```
StarRuptureSaveFixer/
├── Program.cs              # Main entry point and CLI handling
├── Fixers/
│   ├── IFixer.cs           # Interface for save file fixers
│   ├── DroneFixer.cs       # Fixes drones with invalid targets
│   └── DroneRemover.cs     # Removes all drones
├── Models/
│   └── SaveFile.cs         # Save file data model
├── Services/
│   └── SaveFileService.cs  # Save file loading/saving with compression
└── Utils/
    ├── ArgumentParser.cs   # Command-line argument parsing
    └── ConsoleLogger.cs    # Colored console output utilities
```

## Contributing

Contributions are welcome! If you encounter a new type of save file corruption, please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/new-fixer`)
3. Implement a new `IFixer` class for the issue
4. Submit a Pull Request

## Disclaimer

**Always backup your save files before using this tool.** While the tool preserves your original save file, unexpected issues may occur. Use at your own risk.

## Credits

- **Author:** AlienX
- **Website:** [StarRupture-Utilities.com](https://starrupture-utilities.com)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

*This tool is not affiliated with the developers of Star Rupture.*
