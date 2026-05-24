using AutoBingSearch.Core.Browsers;

namespace AutoBingSearch.Tests;

public sealed class BrowserCatalogTests
{
    [Fact]
    public void BrowserProfile_ToSelection_PreservesAutomationFields()
    {
        var browser = new BrowserDefinition(
            "edge",
            "Microsoft Edge",
            "C:\\BrowserData",
            "msedge",
            null);
        var profile = new BrowserProfile(browser, "Default", "Personal");

        var selection = profile.ToSelection();

        Assert.Equal("edge", selection.BrowserId);
        Assert.Equal("Microsoft Edge", selection.DisplayName);
        Assert.Equal("Default", selection.ProfileName);
        Assert.Equal("msedge", selection.PlaywrightChannel);
    }
}
