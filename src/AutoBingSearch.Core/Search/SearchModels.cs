namespace AutoBingSearch.Core.Search;

public sealed record SearchProgress(int Current, int Total, string Query, string Status);

public sealed record SearchAttempt(bool Ok, string Status);

public sealed class SearchRunResult
{
    public int Completed { get; init; }
    public int Failed { get; init; }
    public bool ChallengeHit { get; init; }
    public TimeSpan Elapsed { get; init; }
}
