using AutoBingSearch.Core.Configuration;
using System.Globalization;

namespace AutoBingSearch.Core.Scheduling;

public sealed class RunStateStore
{
    private readonly string _path;

    public RunStateStore(string? path = null)
    {
        _path = path ?? AppPaths.SearchStatePath;
    }

    public bool ShouldRunSearchNow(AppConfig config, DateTime now)
    {
        config.Normalize();
        if (!config.SearchEnabled)
            return false;

        if (!TimeOnly.TryParse(config.SearchTime, out var searchTime))
            return false;

        var today = DateOnly.FromDateTime(now);
        if (TimeOnly.FromDateTime(now) < searchTime)
            return false;

        return LoadLastSearchDate() != today;
    }

    public DateOnly? LoadLastSearchDate()
    {
        try
        {
            if (!File.Exists(_path))
                return null;

            var text = File.ReadAllText(_path).Trim();
            return DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
                : null;
        }
        catch
        {
            return null;
        }
    }

    public void MarkSearchRun(DateTime? now = null)
    {
        var date = DateOnly.FromDateTime(now ?? DateTime.Now);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}
