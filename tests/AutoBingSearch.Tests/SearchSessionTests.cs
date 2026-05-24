using AutoBingSearch.Core.Search;

namespace AutoBingSearch.Tests;

public sealed class SearchSessionTests
{
    [Fact]
    public void CreateBingSearchUrl_EncodesQueryForNavigation()
    {
        var url = SearchSession.CreateBingSearchUrl("weather in Kolkata & Delhi");

        Assert.Equal("https://www.bing.com/search?q=weather+in+Kolkata+%26+Delhi&form=QBLH", url);
    }
}
