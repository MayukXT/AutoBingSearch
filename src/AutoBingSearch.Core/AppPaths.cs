namespace AutoBingSearch.Core;

public static class AppPaths
{
    public static string AppDataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AutoBingSearch");

    public static string LocalDataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AutoBingSearch");

    public static string ConfigPath => Path.Combine(AppDataRoot, "config.json");

    public static string LogDirectory => Path.Combine(LocalDataRoot, "Logs");

    public static string AutomationProfilesDirectory => Path.Combine(LocalDataRoot, "BrowserProfiles");

    public static string SearchStatePath => Path.Combine(LocalDataRoot, "search-state.txt");

    public static void EnsureBaseDirectories()
    {
        Directory.CreateDirectory(AppDataRoot);
        Directory.CreateDirectory(LocalDataRoot);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(AutomationProfilesDirectory);
    }
}
