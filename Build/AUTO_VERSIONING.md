# Automatic Versioning from GitHub Releases

This project automatically determines the next version number by querying GitHub releases.

## How It Works

1. **Before each build**, MSBuild queries the GitHub API for the latest release
2. **Extracts the version** from the latest release tag (e.g., `v1.0.7` ? `1.0.7`)
3. **Auto-increments** the patch version (e.g., `1.0.7` ? `1.0.8`)
4. **Builds with the new version** automatically

## Example

```
GitHub Latest Release: v1.0.7
Your Build Version:    1.0.8  (auto-incremented)
```

## Disabling Auto-Versioning

### Temporarily (for one build):
```bash
dotnet build /p:AutoVersionFromGitHub=false
```

### Permanently:
Edit `StarRuptureSaveFixer.csproj` and change:
```xml
<AutoVersionFromGitHub>false</AutoVersionFromGitHub>
```

## Fallback Behavior

If the GitHub API is unavailable (offline, rate-limited, etc.), the build will:
- Use the version specified in the `.csproj` file (currently `1.0.8`)
- Display a warning in the build output
- Continue building successfully

## Build Output

You'll see messages like:
```
Auto-versioning: Building as version 1.0.9
GitHub latest: 1.0.8 -> Next: 1.0.9
```

Or if offline:
```
Could not fetch GitHub version: The operation has timed out.
Using fallback version: 1.0.8
```

## Publishing Workflow

1. **Build** ? Version auto-increments (e.g., to `1.0.9`)
2. **Test** the build
3. **Create GitHub release** with tag `v1.0.9`
4. **Next build** will be `1.0.10`

## Manual Version Override

If you need a specific version (e.g., major version bump):

```bash
dotnet build /p:Version=2.0.0 /p:AutoVersionFromGitHub=false
```

Then update the fallback version in the `.csproj` file to match.

## Technical Details

- **API Endpoint**: `https://api.github.com/repos/AlienXAXS/StarRupture-Save-Manager/releases/latest`
- **Timeout**: 5 seconds (won't slow down builds)
- **No API Key Required**: Uses public GitHub API (60 requests/hour limit)
- **Regex Pattern**: Matches `v1.2.3` or `1.2.3` format

## Troubleshooting

**Q: Build is slow / timing out?**  
A: Disable auto-versioning or check your internet connection.

**Q: I want to skip a version number?**  
A: Create a dummy GitHub release with the version you want to skip.

**Q: Version didn't increment?**  
A: Check that your latest GitHub release has a valid version tag (e.g., `v1.0.7`).
