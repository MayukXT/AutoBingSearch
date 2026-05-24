using AutoBingSearch.App.Forms;
using AutoBingSearch.Core;
using AutoBingSearch.Core.Configuration;
using AutoBingSearch.Core.Logging;
using AutoBingSearch.Core.Scheduling;
using AutoBingSearch.Core.Search;
using AutoBingSearch.Core.Startup;

namespace AutoBingSearch.App;

internal sealed class CommandRunner
{
    private readonly string[] _args;
    private readonly ConfigStore _configStore = new();
    private readonly RunStateStore _runState = new();
    private readonly AppLogger _log = new();

    public CommandRunner(string[] args)
    {
        _args = args;
    }

    public void Run()
    {
        var command = _args.FirstOrDefault()?.ToLowerInvariant();

        switch (command)
        {
            case "--run":
                RunSearch(testMode: false);
                break;
            case "--test":
                RunSearch(testMode: true);
                break;
            case "--reminder":
                Application.Run(new ReminderForm());
                break;
            case "--install":
                Install();
                break;
            case "--uninstall":
                Uninstall();
                break;
            case "--setup":
                RunSetup();
                break;
            case "--tray":
            default:
                RunTray();
                break;
        }
    }

    private void RunTray()
    {
        if (!_configStore.Exists)
            RunSetup();

        if (!_configStore.Exists)
            return;

        Application.Run(new TrayAppContext(_configStore, _log));
    }

    private void RunSetup()
    {
        using var form = new SetupForm(_configStore);
        if (form.ShowDialog() != DialogResult.OK)
            return;

        TryInstall(showSuccess: false);
    }

    private void Install()
    {
        if (!TryInstall(showSuccess: !HasSwitch("--quiet"), allowPrompts: !HasSwitch("--quiet")))
            Environment.ExitCode = 1;
    }

    private bool TryInstall(bool showSuccess, bool allowPrompts = true)
    {
        try
        {
            var exePath = Environment.ProcessPath ?? Application.ExecutablePath;
            new StartupShortcutService().Register(exePath);

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            new TaskSchedulerService()
                .RegisterAsync(_configStore.Load(), exePath, timeout.Token)
                .GetAwaiter()
                .GetResult();

            if (showSuccess)
                MessageBox.Show("Startup and scheduled tasks are registered.", "AutoBingSearch");

            return true;
        }
        catch (Exception ex)
        {
            var scheduler = new TaskSchedulerService();
            if (allowPrompts && !scheduler.IsAdministrator())
            {
                var ask = MessageBox.Show(
                    "Windows blocked scheduled task registration. Run as administrator now?",
                    "AutoBingSearch needs permission",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (ask == DialogResult.Yes)
                {
                    scheduler.RelaunchElevated(Environment.ProcessPath ?? Application.ExecutablePath, "--install");
                    return false;
                }
            }

            if (allowPrompts)
            {
                MessageBox.Show(
                    $"AutoBingSearch could not register unattended startup:\n\n{ex.Message}",
                    "Setup failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return false;
        }
    }

    private bool HasSwitch(string name)
    {
        return _args.Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    }

    private void Uninstall()
    {
        new StartupShortcutService().Unregister();
        new TaskSchedulerService().UnregisterAsync().GetAwaiter().GetResult();
    }

    private void RunSearch(bool testMode)
    {
        using var runLock = new Mutex(initiallyOwned: true, "AutoBingSearch.SearchRun", out var ownsRun);
        if (!ownsRun)
        {
            _log.Warn("Search run skipped because another run is already active.");
            return;
        }

        var config = _configStore.Load();
        if (!testMode && !config.SearchEnabled)
        {
            _log.Warn("Search run skipped because searches are disabled.");
            return;
        }

        Console.WriteLine($"AutoBingSearch {(testMode ? "test" : "scheduled")} run");
        Console.WriteLine($"Browser: {config.Browser.DisplayName} / {config.Browser.ProfileName}");
        Console.WriteLine($"Searches: {(testMode ? 3 : config.SearchCount)}");
        Console.WriteLine($"Log: {_log.LogPath}");

        var progress = new Progress<SearchProgress>(p =>
        {
            Console.WriteLine($"{p.Current,2}/{p.Total} [{p.Status}] {p.Query}");
        });

        var result = new SearchSession(config, _log)
            .RunAsync(testMode, progress)
            .GetAwaiter()
            .GetResult();

        if (!testMode && result.Completed > 0)
            _runState.MarkSearchRun();

        Console.WriteLine($"Done: {result.Completed} OK, {result.Failed} failed in {result.Elapsed:mm\\:ss}");
    }
}
