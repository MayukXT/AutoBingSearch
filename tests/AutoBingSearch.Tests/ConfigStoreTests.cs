using AutoBingSearch.Core.Configuration;

namespace AutoBingSearch.Tests;

public sealed class ConfigStoreTests
{
    [Fact]
    public void Load_ReturnsDefaults_WhenConfigIsMissing()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "config.json");
        var store = new ConfigStore(path);

        var config = store.Load();

        Assert.Equal("22:00", config.SearchTime);
        Assert.Equal("22:30", config.ReminderTime);
        Assert.Equal(30, config.SearchCount);
        Assert.Equal("edge", config.Browser.BrowserId);
    }

    [Fact]
    public void Save_RoundTripsBrowserSelection()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "config.json");
        var store = new ConfigStore(path);
        var config = new AppConfig
        {
            SearchTime = "7:5",
            Browser = new BrowserSelection
            {
                BrowserId = "chrome",
                DisplayName = "Google Chrome",
                UserDataDir = "%LOCALAPPDATA%\\Google\\Chrome\\User Data",
                ProfileName = "Profile 1",
                PlaywrightChannel = "chrome"
            }
        };

        store.Save(config);
        var loaded = store.Load();

        Assert.Equal("07:05", loaded.SearchTime);
        Assert.Equal("chrome", loaded.Browser.BrowserId);
        Assert.Equal("Profile 1", loaded.Browser.ProfileName);
        Assert.DoesNotContain("%LOCALAPPDATA%", loaded.Browser.UserDataDir);
    }
}
