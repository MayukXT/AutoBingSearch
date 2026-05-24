using AutoBingSearch.Core.Browsers;
using AutoBingSearch.Core.Configuration;
using AutoBingSearch.Core.Scheduling;
using AutoBingSearch.Core.Search;
using AutoBingSearch.Core.Startup;

namespace AutoBingSearch.App.Forms;

internal sealed class SettingsForm : Form
{
    private readonly ConfigStore _store;
    private readonly List<BrowserProfile> _profiles;
    private readonly List<BrowserProfile> _browserChoices;
    private readonly ComboBox _browserBox = new();
    private readonly Label _profileSummary = new();
    private readonly TextBox _searchTime = new();
    private readonly TextBox _reminderTime = new();
    private readonly NumericUpDown _searchCount = new();
    private readonly NumericUpDown _delayMin = new();
    private readonly NumericUpDown _delayMax = new();
    private readonly CheckBox _searchEnabled = new();
    private readonly CheckBox _reminderEnabled = new();
    private readonly Button _save = AppTheme.Button("Save", primary: true);
    private readonly Label _status = new();
    private SettingsSnapshot _savedSnapshot = SettingsSnapshot.Empty;
    private bool _loading;
    private bool _saving;

    public SettingsForm(ConfigStore store)
    {
        _store = store;
        _profiles = new BrowserCatalog().GetProfiles().ToList();
        _browserChoices = BrowserProfileSelector.PickBestPerBrowser(_profiles, _store.Load().Browser).ToList();
        Build();
        LoadValues();
    }

    private void Build()
    {
        Text = "AutoBingSearch Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = AppTheme.Background;
        ClientSize = new Size(560, 520);

        var title = new Label
        {
            Text = "Settings",
            Font = AppTheme.TitleFont,
            Location = new Point(30, 22),
            Size = new Size(480, 34)
        };

        var y = 78;
        Controls.Add(title);
        AddLabel("Browser", y);
        _browserBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _browserBox.DataSource = _browserChoices;
        _browserBox.Format += (_, e) =>
        {
            if (e.ListItem is BrowserProfile profile)
                e.Value = profile.Browser.DisplayName;
        };
        _browserBox.SelectedIndexChanged += (_, _) => UpdateProfileSummary();
        AddControl(_browserBox, y);
        y += 34;
        _profileSummary.Font = AppTheme.BodyFont;
        _profileSummary.ForeColor = AppTheme.Muted;
        _profileSummary.Location = new Point(210, y);
        _profileSummary.Size = new Size(310, 24);
        Controls.Add(_profileSummary);
        y += 36;

        AddLabel("Search time", y);
        AddControl(_searchTime, y);
        y += 42;
        AddLabel("Reminder time", y);
        AddControl(_reminderTime, y);
        y += 42;
        AddLabel("Daily searches", y);
        _searchCount.Minimum = 1;
        _searchCount.Maximum = 30;
        AddControl(_searchCount, y);
        y += 42;
        AddLabel("Minimum delay", y);
        _delayMin.Minimum = 1;
        _delayMin.Maximum = 300;
        AddControl(_delayMin, y);
        y += 42;
        AddLabel("Maximum delay", y);
        _delayMax.Minimum = 2;
        _delayMax.Maximum = 300;
        AddControl(_delayMax, y);
        y += 50;

        _searchEnabled.Text = "Enable daily searches";
        _searchEnabled.Location = new Point(210, y);
        _searchEnabled.Size = new Size(250, 28);
        Controls.Add(_searchEnabled);
        y += 32;
        _reminderEnabled.Text = "Enable reminder";
        _reminderEnabled.Location = new Point(210, y);
        _reminderEnabled.Size = new Size(250, 28);
        Controls.Add(_reminderEnabled);

        _status.Font = AppTheme.BodyFont;
        _status.ForeColor = AppTheme.Muted;
        _status.Location = new Point(30, 468);
        _status.Size = new Size(490, 24);
        Controls.Add(_status);

        var reset = AppTheme.Button("Reset copied browser profile");
        reset.Location = new Point(30, 430);
        reset.Size = new Size(210, 38);
        reset.Click += (_, _) => ResetProfile();

        _save.Location = new Point(410, 430);
        _save.Size = new Size(110, 38);
        _save.Click += async (_, _) => await SaveAsync();

        var cancel = AppTheme.Button("Cancel");
        cancel.Location = new Point(280, 430);
        cancel.Size = new Size(110, 38);
        cancel.Click += (_, _) => Close();

        Controls.AddRange([reset, cancel, _save]);
        WireChangeTracking();
        UpdateSaveState();
    }

    private void AddLabel(string text, int y)
    {
        Controls.Add(new Label
        {
            Text = text,
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.Muted,
            Location = new Point(30, y + 4),
            Size = new Size(160, 24)
        });
    }

    private void AddControl(Control control, int y)
    {
        control.Font = AppTheme.BodyFont;
        control.Location = new Point(210, y);
        control.Size = new Size(310, 28);
        Controls.Add(control);
    }

    private void LoadValues()
    {
        _loading = true;
        var config = _store.Load();
        _searchTime.Text = config.SearchTime;
        _reminderTime.Text = config.ReminderTime;
        _searchCount.Value = config.SearchCount;
        _delayMin.Value = (decimal)config.DelayMinSeconds;
        _delayMax.Value = (decimal)config.DelayMaxSeconds;
        _searchEnabled.Checked = config.SearchEnabled;
        _reminderEnabled.Checked = config.ReminderEnabled;

        var match = _browserChoices.FirstOrDefault(p =>
            p.Browser.Id == config.Browser.BrowserId);
        _browserBox.SelectedItem = match ?? _browserChoices.FirstOrDefault();
        UpdateProfileSummary();
        _savedSnapshot = SnapshotFromControls();
        _loading = false;
        UpdateSaveState();
    }

    private async Task SaveAsync()
    {
        if (_saving || !HasUnsavedChanges())
            return;

        if (!TryBuildConfig(showErrors: true, out var config))
            return;

        _saving = true;
        _save.Text = "Saving...";
        _status.Text = "Saving settings...";
        AppTheme.SetButtonState(_save, enabled: false, primary: true);

        _store.Save(config);
        _savedSnapshot = SnapshotFromControls();

        try
        {
            await UpdateWindowsRegistrationAsync(config);
            _status.Text = "Saved. Schedule updated.";
        }
        catch (Exception ex)
        {
            HandleScheduleRegistrationError(ex);
        }
        finally
        {
            _saving = false;
            _save.Text = "Save";
            UpdateSaveState();
        }
    }

    private bool TryBuildConfig(bool showErrors, out AppConfig config)
    {
        config = _store.Load();

        if (!TimeOnly.TryParse(_searchTime.Text, out var searchTime) ||
            !TimeOnly.TryParse(_reminderTime.Text, out var reminderTime))
        {
            if (showErrors)
                MessageBox.Show("Use HH:mm time format.", "Invalid settings");
            return false;
        }

        if (_browserBox.SelectedItem is not BrowserProfile profile)
            return false;

        if (profile.IsGuest)
        {
            if (showErrors)
            {
                MessageBox.Show(
                    "Only a Guest profile was found for this browser. Sign into Bing in a normal profile first, then try again.",
                    "Signed-in profile required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            return false;
        }

        config.Browser = profile.ToSelection();
        config.SearchTime = searchTime.ToString("HH:mm");
        config.ReminderTime = reminderTime.ToString("HH:mm");
        config.SearchCount = (int)_searchCount.Value;
        config.DelayMinSeconds = (double)_delayMin.Value;
        config.DelayMaxSeconds = (double)_delayMax.Value;
        config.SearchEnabled = _searchEnabled.Checked;
        config.ReminderEnabled = _reminderEnabled.Checked;
        return true;
    }

    private static async Task UpdateWindowsRegistrationAsync(AppConfig config)
    {
        var exe = Environment.ProcessPath ?? Application.ExecutablePath;
        new StartupShortcutService().Register(exe);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        await Task.Run(
            () => new TaskSchedulerService().RegisterAsync(config, exe, timeout.Token),
            timeout.Token);
    }

    private void HandleScheduleRegistrationError(Exception ex)
    {
        if (ex is OperationCanceledException)
        {
            _status.Text = "Saved, but schedule update timed out.";
            MessageBox.Show(
                "Settings were saved, but Windows Task Scheduler did not respond in time. Try Save again, or run the app as administrator if Windows keeps blocking the update.",
                "Task Scheduler timeout",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var scheduler = new TaskSchedulerService();
        if (!scheduler.IsAdministrator())
        {
            var ask = MessageBox.Show(
                "Settings were saved, but Windows blocked scheduled task registration. Run as administrator now?",
                "AutoBingSearch needs permission",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (ask == DialogResult.Yes)
            {
                scheduler.RelaunchElevated(Environment.ProcessPath ?? Application.ExecutablePath, "--install");
                _status.Text = "Saved. Administrator setup opened.";
                return;
            }
        }

        _status.Text = "Saved, but schedule was not updated.";
        MessageBox.Show(
            $"Settings were saved, but scheduled tasks were not updated.\n\n{ex.Message}",
            "Task Scheduler",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private void ResetProfile()
    {
        BrowserProfileSeeder.ResetAutomationProfile(_store.Load().Browser);
        MessageBox.Show("Copied browser profile reset. The next run will copy it again.", "AutoBingSearch");
    }

    private void UpdateProfileSummary()
    {
        if (_browserBox.SelectedItem is not BrowserProfile profile)
        {
            _profileSummary.Text = "";
            return;
        }

        _profileSummary.Text = profile.IsGuest
            ? "No normal signed-in profile found."
            : $"Profile: {profile.DisplayName}";
    }

    private void WireChangeTracking()
    {
        _browserBox.SelectedIndexChanged += (_, _) => UpdateSaveState();
        _searchTime.TextChanged += (_, _) => UpdateSaveState();
        _reminderTime.TextChanged += (_, _) => UpdateSaveState();
        _searchCount.ValueChanged += (_, _) => UpdateSaveState();
        _delayMin.ValueChanged += (_, _) => UpdateSaveState();
        _delayMax.ValueChanged += (_, _) => UpdateSaveState();
        _searchEnabled.CheckedChanged += (_, _) => UpdateSaveState();
        _reminderEnabled.CheckedChanged += (_, _) => UpdateSaveState();
    }

    private void UpdateSaveState()
    {
        if (_loading || _saving)
            return;

        var hasChanges = HasUnsavedChanges();
        AppTheme.SetButtonState(_save, hasChanges, primary: true);
        if (hasChanges)
            _status.Text = "Unsaved changes";
        else if (_status.Text == "Unsaved changes")
            _status.Text = "";
    }

    private bool HasUnsavedChanges()
    {
        return SnapshotFromControls() != _savedSnapshot;
    }

    private SettingsSnapshot SnapshotFromControls()
    {
        var selectedProfile = _browserBox.SelectedItem as BrowserProfile;
        return new SettingsSnapshot(
            selectedProfile?.Browser.Id ?? "",
            selectedProfile?.ProfileName ?? "",
            _searchTime.Text.Trim(),
            _reminderTime.Text.Trim(),
            (int)_searchCount.Value,
            (double)_delayMin.Value,
            (double)_delayMax.Value,
            _searchEnabled.Checked,
            _reminderEnabled.Checked);
    }

    private sealed record SettingsSnapshot(
        string BrowserId,
        string ProfileName,
        string SearchTime,
        string ReminderTime,
        int SearchCount,
        double DelayMinSeconds,
        double DelayMaxSeconds,
        bool SearchEnabled,
        bool ReminderEnabled)
    {
        public static SettingsSnapshot Empty { get; } = new("", "", "", "", 0, 0, 0, false, false);
    }
}
