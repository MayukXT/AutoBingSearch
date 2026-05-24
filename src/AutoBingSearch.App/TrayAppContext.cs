using AutoBingSearch.App.Forms;
using AutoBingSearch.Core;
using AutoBingSearch.Core.Configuration;
using AutoBingSearch.Core.Logging;
using AutoBingSearch.Core.Scheduling;
using AutoBingSearch.Core.Search;

namespace AutoBingSearch.App;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly ConfigStore _store;
    private readonly AppLogger _log;
    private readonly RunStateStore _runState = new();
    private readonly NotifyIcon _icon;
    private readonly System.Windows.Forms.Timer _startupTimer;
    private readonly SynchronizationContext _uiContext;
    private bool _running;

    public TrayAppContext(ConfigStore store, AppLogger log)
    {
        _store = store;
        _log = log;
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _icon = new NotifyIcon
        {
            Icon = AppTheme.LoadAppIcon(),
            Text = Tooltip(),
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _startupTimer = new System.Windows.Forms.Timer { Interval = 1500 };
        _startupTimer.Tick += async (_, _) => await RunMissedSearchAfterStartupAsync();
        _startupTimer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _icon.Visible = false;
            _icon.Dispose();
            _startupTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Run now", null, async (_, _) => await RunSearchAsync(testMode: false));
        menu.Items.Add("Test 3 searches", null, async (_, _) => await RunSearchAsync(testMode: true));
        menu.Items.Add("Show reminder", null, (_, _) => new ReminderForm().Show());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings", null, (_, _) => OpenSettings(menu));
        menu.Items.Add("Open logs", null, (_, _) => System.Diagnostics.Process.Start("explorer.exe", AppPaths.LogDirectory));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        return menu;
    }

    private void OpenSettings(ContextMenuStrip menu)
    {
        menu.Close(ToolStripDropDownCloseReason.ItemClicked);
        _uiContext.Post(_ =>
        {
            using var form = new SettingsForm(_store);
            form.ShowDialog();
            _icon.Text = Tooltip();
        }, null);
    }

    private async Task RunSearchAsync(bool testMode)
    {
        using var runLock = new Mutex(initiallyOwned: true, "AutoBingSearch.SearchRun", out var ownsRun);
        if (_running)
        {
            _icon.ShowBalloonTip(2500, "AutoBingSearch", "A search run is already active.", ToolTipIcon.Info);
            return;
        }

        if (!ownsRun)
        {
            _icon.ShowBalloonTip(2500, "AutoBingSearch", "Another search run is already active.", ToolTipIcon.Info);
            return;
        }

        _running = true;
        _icon.Text = "AutoBingSearch - running";

        try
        {
            var config = _store.Load();
            var progress = new Progress<SearchProgress>(p =>
            {
                _icon.Text = $"AutoBingSearch - {p.Current}/{p.Total} {p.Status}";
            });
            var result = await new SearchSession(config, _log).RunAsync(testMode, progress);
            if (!testMode && result.Completed > 0)
                _runState.MarkSearchRun();

            _icon.ShowBalloonTip(
                5000,
                "AutoBingSearch",
                $"{result.Completed} OK, {result.Failed} failed in {result.Elapsed:mm\\:ss}",
                result.Failed == 0 ? ToolTipIcon.Info : ToolTipIcon.Warning);
        }
        catch (Exception ex)
        {
            _log.Error("Tray search run failed", ex);
            _icon.ShowBalloonTip(5000, "AutoBingSearch error", ex.Message, ToolTipIcon.Error);
        }
        finally
        {
            _running = false;
            _icon.Text = Tooltip();
        }
    }

    private async Task RunMissedSearchAfterStartupAsync()
    {
        _startupTimer.Stop();

        try
        {
            var config = _store.Load();
            if (!_runState.ShouldRunSearchNow(config, DateTime.Now))
                return;

            _icon.ShowBalloonTip(3500, "AutoBingSearch", "Running missed scheduled searches now.", ToolTipIcon.Info);
            await RunSearchAsync(testMode: false);
        }
        catch (Exception ex)
        {
            _log.Error("Startup missed-run check failed", ex);
        }
    }

    private string Tooltip()
    {
        var config = _store.Load();
        return $"AutoBingSearch - {config.Browser.DisplayName} {config.Browser.ProfileName}";
    }
}
