namespace AutoBingSearch.Core.Search;

public sealed class Humanizer
{
    private readonly Random _random = new();
    private readonly DateTimeOffset _startedAt = DateTimeOffset.Now;
    private int _searchesDone;

    public Humanizer(double minDelaySeconds, double maxDelaySeconds, double sessionMaxMinutes, int searchCount)
    {
        MinDelaySeconds = minDelaySeconds;
        MaxDelaySeconds = maxDelaySeconds;
        SessionMaxSeconds = sessionMaxMinutes * 60;
        SearchCount = searchCount;
    }

    public double MinDelaySeconds { get; }
    public double MaxDelaySeconds { get; }
    public double SessionMaxSeconds { get; }
    public int SearchCount { get; }

    public void RecordSearchDone() => _searchesDone++;

    public TimeSpan NextDelay()
    {
        var elapsed = (DateTimeOffset.Now - _startedAt).TotalSeconds;
        var remainingSearches = Math.Max(1, SearchCount - _searchesDone);
        var remainingTime = Math.Max(0, SessionMaxSeconds - elapsed);
        var budgetPerSearch = remainingTime / remainingSearches;

        var effectiveMax = Math.Min(MaxDelaySeconds, Math.Max(2, budgetPerSearch - 3));
        var effectiveMin = Math.Min(MinDelaySeconds, effectiveMax);
        var baseDelay = effectiveMin + (_random.NextDouble() * (effectiveMax - effectiveMin));
        var jitter = (_random.NextDouble() - 0.5) * 2.0;

        return TimeSpan.FromSeconds(Math.Clamp(baseDelay + jitter, 2, MaxDelaySeconds + 3));
    }

    public int TypeDelayMilliseconds() => _random.Next(35, 155);
}
