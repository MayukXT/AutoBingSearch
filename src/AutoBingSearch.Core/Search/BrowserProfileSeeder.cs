using AutoBingSearch.Core.Configuration;

namespace AutoBingSearch.Core.Search;

public static class BrowserProfileSeeder
{
    public static string EnsureAutomationProfile(BrowserSelection browser)
    {
        AppPaths.EnsureBaseDirectories();
        var destinationRoot = Path.Combine(AppPaths.AutomationProfilesDirectory, browser.BrowserId);
        var destinationProfile = Path.Combine(destinationRoot, browser.ProfileName);

        if (Directory.Exists(destinationProfile))
            return destinationRoot;

        Directory.CreateDirectory(destinationProfile);
        CopyProfileEssentials(browser.UserDataDir, browser.ProfileName, destinationRoot, destinationProfile);
        return destinationRoot;
    }

    public static void ResetAutomationProfile(BrowserSelection browser)
    {
        var destinationRoot = Path.Combine(AppPaths.AutomationProfilesDirectory, browser.BrowserId);
        if (Directory.Exists(destinationRoot))
            Directory.Delete(destinationRoot, recursive: true);
    }

    private static void CopyProfileEssentials(
        string sourceUserDataDir,
        string sourceProfileName,
        string destinationRoot,
        string destinationProfile)
    {
        var sourceProfile = Path.Combine(sourceUserDataDir, sourceProfileName);
        SafeCopy(Path.Combine(sourceUserDataDir, "Local State"), Path.Combine(destinationRoot, "Local State"));

        foreach (var file in new[] { "Preferences", "Web Data", "Login Data", "Bookmarks", "DIPS", "Shortcuts" })
            SafeCopy(Path.Combine(sourceProfile, file), Path.Combine(destinationProfile, file));

        var sourceNetwork = Path.Combine(sourceProfile, "Network");
        var destinationNetwork = Path.Combine(destinationProfile, "Network");
        Directory.CreateDirectory(destinationNetwork);
        SafeCopy(Path.Combine(sourceNetwork, "Cookies"), Path.Combine(destinationNetwork, "Cookies"));
        SafeCopy(Path.Combine(sourceNetwork, "Cookies-journal"), Path.Combine(destinationNetwork, "Cookies-journal"));
    }

    private static void SafeCopy(string source, string destination)
    {
        try
        {
            if (!File.Exists(source) || File.Exists(destination))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: false);
        }
        catch
        {
            // Source browser may be open and locking some files. A fresh profile can still launch.
        }
    }
}
