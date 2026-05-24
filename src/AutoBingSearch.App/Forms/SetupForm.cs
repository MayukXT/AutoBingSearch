using AutoBingSearch.Core.Browsers;
using AutoBingSearch.Core.Configuration;

namespace AutoBingSearch.App.Forms;

internal sealed class SetupForm : Form
{
    private readonly ConfigStore _store;
    private readonly ComboBox _browserBox = new();
    private readonly Label _profileHint = new();
    private readonly List<BrowserProfile> _profiles;
    private readonly List<BrowserProfile> _browserChoices;

    public SetupForm(ConfigStore store)
    {
        _store = store;
        _profiles = new BrowserCatalog().GetProfiles().ToList();
        _browserChoices = BrowserProfileSelector.PickBestPerBrowser(_profiles, _store.Load().Browser).ToList();
        Build();
        UpdateProfileHint();
    }

    private void Build()
    {
        Text = "AutoBingSearch Setup";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.Background;
        ClientSize = new Size(520, 300);

        var title = new Label
        {
            Text = "Choose the browser with your Bing account",
            Font = AppTheme.TitleFont,
            ForeColor = AppTheme.Text,
            AutoSize = false,
            Location = new Point(30, 28),
            Size = new Size(460, 34)
        };

        var help = new Label
        {
            Text = "Pick the browser where your Bing account is already signed in. AutoBingSearch skips Guest profiles and uses the best real profile automatically.",
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.Muted,
            AutoSize = false,
            Location = new Point(32, 76),
            Size = new Size(450, 72)
        };

        _browserBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _browserBox.Font = AppTheme.BodyFont;
        _browserBox.Location = new Point(32, 158);
        _browserBox.Size = new Size(450, 28);
        _browserBox.DataSource = _browserChoices;
        _browserBox.Format += (_, e) =>
        {
            if (e.ListItem is BrowserProfile profile)
                e.Value = profile.Browser.DisplayName;
        };
        _browserBox.SelectedIndexChanged += (_, _) => UpdateProfileHint();

        _profileHint.Font = AppTheme.BodyFont;
        _profileHint.ForeColor = AppTheme.Muted;
        _profileHint.Location = new Point(34, 196);
        _profileHint.Size = new Size(448, 24);

        var save = AppTheme.Button("Save and enable unattended runs", primary: true);
        save.Location = new Point(210, 232);
        save.Size = new Size(272, 38);
        save.Click += (_, _) => Save();

        var cancel = AppTheme.Button("Cancel");
        cancel.Location = new Point(32, 232);
        cancel.Size = new Size(150, 38);
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        Controls.AddRange([title, help, _browserBox, _profileHint, cancel, save]);
    }

    private void Save()
    {
        if (_browserBox.SelectedItem is not BrowserProfile profile)
        {
            MessageBox.Show("Pick a browser first.", "AutoBingSearch");
            return;
        }

        if (profile.IsGuest)
        {
            MessageBox.Show(
                "Only a Guest profile was found for this browser. Sign into Bing in a normal profile first, then run setup again.",
                "Signed-in profile required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var config = _store.Load();
        config.Browser = profile.ToSelection();
        _store.Save(config);
        DialogResult = DialogResult.OK;
    }

    private void UpdateProfileHint()
    {
        if (_browserBox.SelectedItem is not BrowserProfile profile)
        {
            _profileHint.Text = "";
            return;
        }

        _profileHint.Text = profile.IsGuest
            ? "No normal signed-in profile found for this browser."
            : $"Using profile: {profile.DisplayName}";
    }
}
