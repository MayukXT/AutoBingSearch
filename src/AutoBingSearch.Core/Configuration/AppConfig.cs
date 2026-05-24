namespace AutoBingSearch.Core.Configuration;

public sealed class AppConfig
{
    public const int MinSearchCount = 1;
    public const int MaxSearchCount = 100;

    public string SearchTime { get; set; } = "22:00";
    public string ReminderTime { get; set; } = "22:30";
    public int SearchCount { get; set; } = 30;
    public double DelayMinSeconds { get; set; } = 10;
    public double DelayMaxSeconds { get; set; } = 30;
    public double SessionMaxMinutes { get; set; } = 20;
    public bool SearchEnabled { get; set; } = true;
    public bool ReminderEnabled { get; set; } = true;
    public BrowserSelection Browser { get; set; } = BrowserSelection.EdgeDefault();

    public void Normalize()
    {
        SearchTime = NormalizeTime(SearchTime, "22:00");
        ReminderTime = NormalizeTime(ReminderTime, "22:30");
        SearchCount = Math.Clamp(SearchCount, MinSearchCount, MaxSearchCount);
        DelayMinSeconds = Math.Clamp(DelayMinSeconds, 1, 300);
        DelayMaxSeconds = Math.Clamp(DelayMaxSeconds, DelayMinSeconds + 1, 300);
        SessionMaxMinutes = Math.Clamp(SessionMaxMinutes, 3, 180);
        Browser ??= BrowserSelection.EdgeDefault();
        Browser.Normalize();
    }

    private static string NormalizeTime(string? value, string fallback)
    {
        return TimeOnly.TryParse(value, out var time) ? time.ToString("HH:mm") : fallback;
    }
}

public sealed class BrowserSelection
{
    public string BrowserId { get; set; } = "edge";
    public string DisplayName { get; set; } = "Microsoft Edge";
    public string UserDataDir { get; set; } = "";
    public string ProfileName { get; set; } = "Default";
    public string? PlaywrightChannel { get; set; } = "msedge";
    public string? ExecutablePath { get; set; }

    public static BrowserSelection EdgeDefault()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new BrowserSelection
        {
            BrowserId = "edge",
            DisplayName = "Microsoft Edge",
            UserDataDir = Path.Combine(localAppData, "Microsoft", "Edge", "User Data"),
            ProfileName = "Default",
            PlaywrightChannel = "msedge",
            ExecutablePath = FirstExisting(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"))
        };
    }

    public void Normalize()
    {
        BrowserId = string.IsNullOrWhiteSpace(BrowserId) ? "edge" : BrowserId.Trim();
        DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? BrowserId : DisplayName.Trim();
        ProfileName = string.IsNullOrWhiteSpace(ProfileName) ? "Default" : ProfileName.Trim();
        UserDataDir = Environment.ExpandEnvironmentVariables(UserDataDir ?? "");
        PlaywrightChannel = string.IsNullOrWhiteSpace(PlaywrightChannel) ? null : PlaywrightChannel.Trim();
        ExecutablePath = string.IsNullOrWhiteSpace(ExecutablePath)
            ? null
            : Environment.ExpandEnvironmentVariables(ExecutablePath.Trim());
    }

    private static string? FirstExisting(params string[] paths)
    {
        return paths.FirstOrDefault(File.Exists);
    }
}
