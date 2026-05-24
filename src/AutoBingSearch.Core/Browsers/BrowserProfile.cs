using AutoBingSearch.Core.Configuration;

namespace AutoBingSearch.Core.Browsers;

public sealed record BrowserDefinition(
    string Id,
    string DisplayName,
    string UserDataDir,
    string? PlaywrightChannel,
    string? ExecutablePath);

public sealed record BrowserProfile
{
    public BrowserProfile(
        BrowserDefinition browser,
        string profileName,
        string displayName,
        bool isGuest = false,
        bool looksSignedIn = false)
    {
        Browser = browser;
        ProfileName = profileName;
        DisplayName = displayName;
        IsGuest = isGuest;
        LooksSignedIn = looksSignedIn;
    }

    public BrowserDefinition Browser { get; }
    public string ProfileName { get; }
    public string DisplayName { get; }
    public bool IsGuest { get; }
    public bool LooksSignedIn { get; }

    public BrowserSelection ToSelection() => new()
    {
        BrowserId = Browser.Id,
        DisplayName = Browser.DisplayName,
        UserDataDir = Browser.UserDataDir,
        ProfileName = ProfileName,
        PlaywrightChannel = Browser.PlaywrightChannel,
        ExecutablePath = Browser.ExecutablePath
    };

    public override string ToString() => $"{Browser.DisplayName} - {DisplayName}";
}
