namespace AutoBingSearch.Core.Startup;

public sealed class StartupShortcutService
{
    private const string ShortcutName = "AutoBingSearch.lnk";

    public string ShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
        ShortcutName);

    public void Register(string exePath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is not available.");

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(ShortcutPath);
        shortcut.TargetPath = exePath;
        shortcut.Arguments = "--tray";
        shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
        shortcut.Description = "Start AutoBingSearch in the system tray";
        shortcut.IconLocation = exePath;
        shortcut.Save();
    }

    public void Unregister()
    {
        if (File.Exists(ShortcutPath))
            File.Delete(ShortcutPath);
    }
}
