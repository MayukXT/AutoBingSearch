using AutoBingSearch.Core.Browsers;

namespace AutoBingSearch.Tests;

public sealed class BrowserProfileSelectorTests
{
    [Fact]
    public void PickBestPerBrowser_SkipsGuest_WhenRealProfileExists()
    {
        var edge = new BrowserDefinition("edge", "Microsoft Edge", "C:\\Edge", "msedge", "msedge.exe");
        var profiles = new[]
        {
            new BrowserProfile(edge, "Guest Profile", "Guest", isGuest: true, looksSignedIn: false),
            new BrowserProfile(edge, "Default", "Search", isGuest: false, looksSignedIn: true)
        };

        var selected = BrowserProfileSelector.PickBestPerBrowser(profiles).Single();

        Assert.Equal("Default", selected.ProfileName);
    }

    [Fact]
    public void PickBestPerBrowser_PrefersSavedProfile_WhenItIsUsable()
    {
        var brave = new BrowserDefinition("brave", "Brave", "C:\\Brave", null, "brave.exe");
        var profiles = new[]
        {
            new BrowserProfile(brave, "Default", "Personal", isGuest: false, looksSignedIn: true),
            new BrowserProfile(brave, "Profile 1", "Work", isGuest: false, looksSignedIn: true)
        };
        var saved = profiles[1].ToSelection();

        var selected = BrowserProfileSelector.PickBestPerBrowser(profiles, saved).Single();

        Assert.Equal("Profile 1", selected.ProfileName);
    }
}
