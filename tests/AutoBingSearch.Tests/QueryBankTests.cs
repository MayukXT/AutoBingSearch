using AutoBingSearch.Core.Search;

namespace AutoBingSearch.Tests;

public sealed class QueryBankTests
{
    [Fact]
    public void PickDailyQueries_IsStableForTheSameDate()
    {
        var date = new DateOnly(2026, 5, 23);

        var first = QueryBank.PickDailyQueries(10, date);
        var second = QueryBank.PickDailyQueries(10, date);

        Assert.Equal(first, second);
        Assert.Equal(10, first.Count);
    }

    [Fact]
    public void PickDailyQueries_ClampsToThirty()
    {
        var queries = QueryBank.PickDailyQueries(100, new DateOnly(2026, 5, 23));

        Assert.Equal(30, queries.Count);
        Assert.Equal(queries.Count, queries.Distinct().Count());
    }
}
