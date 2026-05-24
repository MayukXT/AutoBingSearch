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

        var reset = AppTheme.Button("Reset copied browser profile");
        reset.Location = new Point(30, 430);
        reset.Size = new Size(210, 38);
        reset.Click += (_, _) => ResetProfile();

        var save = AppTheme.Button("Save", primary: true);
        save.Location = new Point(410, 430);
        save.Size = new Size(110, 38);
        save.Click += (_, _) => Save();

        var cancel = AppTheme.Button("Cancel");
        cancel.Location = new Point(280, 430);
        cancel.Size = new Size(110, 38);
        cancel.Click += (_, _) => Close();

        Controls.AddRange([reset, cancel, save]);
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
    }

    private void Save()
    {
        if (!TimeOnly.TryParse(_searchTime.Text, out var searchTime) ||
            !TimeOnly.TryParse(_reminderTime.Text, out var reminderTime))
        {
            MessageBox.Show("Use HH:mm time format.", "Invalid settings");
            return;
        }

        if (_browserBox.SelectedItem is not BrowserProfile profile)
            return;

        if (profile.IsGuest)
        {
            MessageBox.Show(
                "Only a Guest profile was found for this browser. Sign into Bing in a normal profile first, then try again.",
                "Signed-in profile required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var config = _store.Load();
        config.Browser = profile.ToSelection();
        config.SearchTime = searchTime.ToString("HH:mm");
        config.ReminderTime = reminderTime.ToString("HH:mm");
        config.SearchCount = (int)_searchCount.Value;
        config.DelayMinSeconds = (double)_delayMin.Value;
        config.DelayMaxSeconds = (double)_delayMax.Value;
        config.SearchEnabled = _searchEnabled.Checked;
        config.ReminderEnabled = _reminderEnabled.Checked;
        _store.Save(config);

        try
        {
            var exe = Environment.ProcessPath ?? Application.ExecutablePath;
            new StartupShortcutService().Register(exe);
            new TaskSchedulerService().RegisterAsync(config, exe).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
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
                    Close();
                    return;
                }
            }

            MessageBox.Show(
                $"Settings were saved, but scheduled tasks were not updated.\n\n{ex.Message}",
                "Task Scheduler",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        Close();
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
}
