using System.Text.Json;

namespace AutoBingSearch.Core.Configuration;

public sealed class ConfigStore
{
    private readonly string _path;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public ConfigStore(string? path = null)
    {
        _path = path ?? AppPaths.ConfigPath;
    }

    public bool Exists => File.Exists(_path);

    public AppConfig Load()
    {
        if (!File.Exists(_path))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(_path);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            config.Normalize();
            return config;
        }
        catch (JsonException)
        {
            return new AppConfig();
        }
        catch (IOException)
        {
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        config.Normalize();
        AppPaths.EnsureBaseDirectories();
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(config, JsonOptions));
    }
}
