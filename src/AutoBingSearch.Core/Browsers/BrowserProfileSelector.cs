using AutoBingSearch.Core.Configuration;

namespace AutoBingSearch.Core.Browsers;

public static class BrowserProfileSelector
{
    public static IReadOnlyList<BrowserProfile> PickBestPerBrowser(
        IEnumerable<BrowserProfile> profiles,
        BrowserSelection? savedSelection = null)
    {
        return profiles
            .GroupBy(p => p.Browser.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => PickBest(group, savedSelection))
            .Where(profile => profile is not null)
            .Cast<BrowserProfile>()
            .OrderBy(ProfileBrowserSortKey)
            .ThenBy(p => p.Browser.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static BrowserProfile? PickBest(
        IEnumerable<BrowserProfile> profiles,
        BrowserSelection? savedSelection = null)
    {
        return profiles
            .OrderByDescending(p => MatchesSavedProfile(p, savedSelection))
            .ThenBy(p => p.IsGuest)
            .ThenByDescending(p => p.LooksSignedIn)
            .ThenBy(ProfileNameSortKey)
            .ThenBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static bool MatchesSavedProfile(BrowserProfile profile, BrowserSelection? saved)
    {
        if (saved is null || profile.IsGuest)
            return false;

        return string.Equals(profile.Browser.Id, saved.BrowserId, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(profile.ProfileName, saved.ProfileName, StringComparison.OrdinalIgnoreCase);
    }

    private static int ProfileBrowserSortKey(BrowserProfile profile)
    {
        return profile.Browser.Id.ToLowerInvariant() switch
        {
            "edge" => 0,
            "chrome" => 1,
            "brave" => 2,
            _ => 10
        };
    }

    private static int ProfileNameSortKey(BrowserProfile profile)
    {
        if (profile.ProfileName.Equals("Default", StringComparison.OrdinalIgnoreCase))
            return 0;

        return profile.ProfileName.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
    }
}
