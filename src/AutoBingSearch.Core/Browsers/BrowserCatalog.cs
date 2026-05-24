using System.Text.Json;

namespace AutoBingSearch.Core.Browsers;

public sealed class BrowserCatalog
{
    public IReadOnlyList<BrowserDefinition> GetInstalledBrowsers()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        var candidates = new List<BrowserDefinition>
        {
            new(
                "edge",
                "Microsoft Edge",
                Path.Combine(local, "Microsoft", "Edge", "User Data"),
                "msedge",
                FirstExisting(
                    Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"),
                    Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe"))),
            new(
                "chrome",
                "Google Chrome",
                Path.Combine(local, "Google", "Chrome", "User Data"),
                "chrome",
                FirstExisting(
                    Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"),
                    Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe"))),
            new(
                "brave",
                "Brave",
                Path.Combine(local, "BraveSoftware", "Brave-Browser", "User Data"),
                null,
                Path.Combine(programFiles, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"))
        };

        return candidates
            .Where(b => Directory.Exists(b.UserDataDir) || BrowserExecutableExists(b))
            .DefaultIfEmpty(candidates[0])
            .ToList();
    }

    public IReadOnlyList<BrowserProfile> GetProfiles()
    {
        var profiles = new List<BrowserProfile>();

        foreach (var browser in GetInstalledBrowsers())
        {
            if (!Directory.Exists(browser.UserDataDir))
            {
                profiles.Add(new BrowserProfile(browser, "Default", "Default profile"));
                continue;
            }

            var metadata = ReadProfileMetadata(browser.UserDataDir);
            var dirs = Directory.EnumerateDirectories(browser.UserDataDir)
                .Select(Path.GetFileName)
                .Where(name => IsProfileDirectory(browser.UserDataDir, name))
                .OrderBy(ProfileSortKey)
                .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (dirs.Count == 0)
                dirs.Add("Default");

            foreach (var profile in dirs)
            {
                var info = metadata.GetValueOrDefault(profile!);
                var displayName = string.IsNullOrWhiteSpace(info?.Name) ? profile! : info.Name;
                profiles.Add(new BrowserProfile(
                    browser,
                    profile!,
                    displayName,
                    IsGuestProfile(profile!, displayName),
                    info?.LooksSignedIn == true));
            }
        }

        return profiles;
    }

    private static bool BrowserExecutableExists(BrowserDefinition browser)
    {
        return !string.IsNullOrWhiteSpace(browser.ExecutablePath) && File.Exists(browser.ExecutablePath);
    }

    private static string? FirstExisting(params string[] paths)
    {
        return paths.FirstOrDefault(File.Exists);
    }

    private static bool IsProfileDirectory(string userDataDir, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (name.Equals("Default", StringComparison.OrdinalIgnoreCase))
            return true;

        if (name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase))
            return true;

        return File.Exists(Path.Combine(userDataDir, name, "Preferences"));
    }

    private static int ProfileSortKey(string? name)
    {
        if (string.Equals(name, "Default", StringComparison.OrdinalIgnoreCase))
            return 0;

        return name?.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase) == true ? 1 : 2;
    }

    private static Dictionary<string, ProfileMetadata> ReadProfileMetadata(string userDataDir)
    {
        var profiles = new Dictionary<string, ProfileMetadata>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var localState = Path.Combine(userDataDir, "Local State");
            if (!File.Exists(localState))
                return profiles;

            using var document = JsonDocument.Parse(File.ReadAllText(localState));
            if (!document.RootElement.TryGetProperty("profile", out var profileRoot) ||
                !profileRoot.TryGetProperty("info_cache", out var infoCache))
            {
                return profiles;
            }

            foreach (var profile in infoCache.EnumerateObject())
            {
                var name = ReadString(profile.Value, "name");
                var looksSignedIn =
                    HasNonEmptyString(profile.Value, "user_name") ||
                    HasNonEmptyString(profile.Value, "gaia_name") ||
                    HasNonEmptyString(profile.Value, "gaia_id") ||
                    HasNonEmptyString(profile.Value, "email") ||
                    ReadBool(profile.Value, "is_consented_primary_account");

                profiles[profile.Name] = new ProfileMetadata(name, looksSignedIn);
            }
        }
        catch
        {
            return profiles;
        }

        return profiles;
    }

    private static bool IsGuestProfile(string profileName, string displayName)
    {
        return profileName.Contains("Guest", StringComparison.OrdinalIgnoreCase) ||
               displayName.Contains("Guest", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool ReadBool(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) &&
               value.ValueKind is JsonValueKind.True or JsonValueKind.False &&
               value.GetBoolean();
    }

    private static bool HasNonEmptyString(JsonElement element, string name)
    {
        return !string.IsNullOrWhiteSpace(ReadString(element, name));
    }

    private sealed record ProfileMetadata(string? Name, bool LooksSignedIn);
}
