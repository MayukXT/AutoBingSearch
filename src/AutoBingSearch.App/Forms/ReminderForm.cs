namespace AutoBingSearch.App.Forms;

internal sealed class ReminderForm : Form
{
    private readonly Button _acknowledge;
    private readonly Button _confirm;
    private readonly Label _status;
    private readonly System.Windows.Forms.Timer _timer = new();
    private int _remaining = 10;
    private bool _acknowledged;

    public ReminderForm()
    {
        Text = "AutoBingSearch Reminder";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        BackColor = Color.FromArgb(8, 8, 8);
        ClientSize = new Size(500, 390);
        KeyPreview = true;

        var title = new Label
        {
            Text = "Bing searches are ready",
            Font = new Font("Bahnschrift", 24, FontStyle.Bold),
            ForeColor = Color.FromArgb(247, 242, 233),
            Location = new Point(38, 70),
            Size = new Size(430, 42)
        };

        var body = new Label
        {
            Text = "Acknowledge this reminder, wait 10 seconds, then press Confirm to clear it from your screen.",
            Font = new Font("Candara", 12),
            ForeColor = Color.FromArgb(206, 197, 182),
            Location = new Point(40, 126),
            Size = new Size(410, 64)
        };

        _status = new Label
        {
            Text = "Waiting for acknowledgement",
            Font = AppTheme.ButtonFont,
            ForeColor = Color.FromArgb(214, 196, 167),
            Location = new Point(40, 206),
            Size = new Size(400, 26)
        };

        _acknowledge = AppTheme.Button("Acknowledge");
        _acknowledge.Location = new Point(40, 254);
        _acknowledge.Size = new Size(420, 45);
        _acknowledge.Click += (_, _) => Acknowledge();

        _confirm = AppTheme.Button("Confirm in 10s", primary: true);
        _confirm.Location = new Point(40, 317);
        _confirm.Size = new Size(420, 45);
        _confirm.Enabled = false;
        _confirm.Click += (_, _) => Confirm();

        Controls.AddRange([title, body, _status, _acknowledge, _confirm]);
        _timer.Interval = 1000;
        _timer.Tick += (_, _) => Tick();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_acknowledged || _remaining > 0)
        {
            e.Cancel = true;
            Activate();
            return;
        }

        base.OnFormClosing(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData is Keys.Escape or (Keys.Alt | Keys.F4))
        {
            Activate();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void Acknowledge()
    {
        if (_acknowledged)
            return;

        _acknowledged = true;
        _acknowledge.Enabled = false;
        _acknowledge.Text = "Acknowledged";
        _status.Text = "Confirmation unlocks in 10s";
        _timer.Start();
    }

    private void Tick()
    {
        _remaining--;
        if (_remaining > 0)
        {
            _status.Text = $"Confirmation unlocks in {_remaining}s";
            _confirm.Text = $"Confirm in {_remaining}s";
            return;
        }

        _timer.Stop();
        _status.Text = "Ready to clear";
        _confirm.Text = "Confirm";
        _confirm.Enabled = true;
    }

    private void Confirm()
    {
        if (_remaining > 0)
            return;

        _acknowledged = true;
        Close();
    }
}
