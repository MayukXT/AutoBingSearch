using AutoBingSearch.Core.Configuration;
using AutoBingSearch.Core.Scheduling;

namespace AutoBingSearch.Tests;

public sealed class RunStateStoreTests
{
    [Fact]
    public void ShouldRunSearchNow_ReturnsTrue_WhenSearchTimeAlreadyPassedAndNoRunToday()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "state.txt");
        var store = new RunStateStore(path);
        var config = new AppConfig { SearchTime = "22:00" };

        var due = store.ShouldRunSearchNow(config, new DateTime(2026, 5, 23, 22, 15, 0));

        Assert.True(due);
    }

    [Fact]
    public void ShouldRunSearchNow_ReturnsFalse_AfterTodayIsMarked()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "state.txt");
        var store = new RunStateStore(path);
        var now = new DateTime(2026, 5, 23, 22, 15, 0);
        var config = new AppConfig { SearchTime = "22:00" };

        store.MarkSearchRun(now);
        var due = store.ShouldRunSearchNow(config, now);

        Assert.False(due);
    }
}
