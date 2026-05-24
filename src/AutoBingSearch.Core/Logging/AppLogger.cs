namespace AutoBingSearch.Core.Logging;

public sealed class AppLogger
{
    private readonly string _path;
    private readonly object _gate = new();

    public AppLogger(string? logDirectory = null)
    {
        var directory = logDirectory ?? AppPaths.LogDirectory;
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, $"autobingsearch-{DateTime.Now:yyyy-MM-dd}.log");
    }

    public string LogPath => _path;

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
    {
        var text = exception is null ? message : $"{message} | {exception.GetType().Name}: {exception.Message}";
        Write("ERROR", text);
    }

    private void Write(string level, string message)
    {
        lock (_gate)
        {
            File.AppendAllText(_path, $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}");
        }
    }
}
