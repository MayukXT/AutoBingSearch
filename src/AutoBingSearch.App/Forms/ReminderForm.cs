using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace AutoBingSearch.App.Forms;

internal sealed class ReminderForm : Form
{
    private static readonly IntPtr HwndTopMost = new(-1);

    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpShowWindow = 0x0040;

    private readonly System.Windows.Forms.Timer _countdownTimer = new();
    private readonly System.Windows.Forms.Timer _animationTimer = new();
    private readonly System.Windows.Forms.Timer _focusTimer = new();
    private readonly Font _eyebrowFont = CreateFont(["Cascadia Code", "Bahnschrift", "Candara"], 9.5f, FontStyle.Bold);
    private readonly Font _titleFont = CreateFont(["Bahnschrift SemiCondensed", "Bahnschrift", "Candara"], 31f, FontStyle.Bold);
    private readonly Font _bodyFont = CreateFont(["Candara", "Bahnschrift", "Microsoft Sans Serif"], 13.5f, FontStyle.Regular);
    private readonly Font _buttonFont = CreateFont(["Bahnschrift", "Candara", "Microsoft Sans Serif"], 12.5f, FontStyle.Bold);
    private readonly Font _metaFont = CreateFont(["Cascadia Code", "Bahnschrift", "Candara"], 10.5f, FontStyle.Regular);
    private RectangleF _acknowledgeBounds;
    private RectangleF _confirmBounds;
    private RectangleF _cardBounds;
    private ReminderButton _hoveredButton = ReminderButton.None;
    private float _introProgress;
    private float _pulse;
    private int _remainingSeconds = 10;
    private bool _acknowledged;
    private bool _allowClose;

    public ReminderForm()
    {
        Text = "AutoBingSearch Reminder";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        DoubleBuffered = true;
        BackColor = Color.FromArgb(6, 7, 8);
        ClientSize = new Size(640, 430);
        Cursor = Cursors.Default;

        _countdownTimer.Interval = 1000;
        _countdownTimer.Tick += (_, _) => TickCountdown();

        _animationTimer.Interval = 16;
        _animationTimer.Tick += (_, _) => TickAnimation();
        _animationTimer.Start();

        _focusTimer.Interval = 650;
        _focusTimer.Tick += (_, _) => KeepOverlayInFront();
        _focusTimer.Start();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        CenterOnCurrentScreen();
        KeepOverlayInFront();
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        if (!_allowClose)
            BeginInvoke(KeepOverlayInFront);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            KeepOverlayInFront();
            return;
        }

        base.OnFormClosing(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_allowClose && IsBlockedShortcut(keyData))
        {
            KeepOverlayInFront();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ApplyWindowShape();
        LayoutOverlay();
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var next = HitTest(e.Location);
        if (next == _hoveredButton)
            return;

        _hoveredButton = next;
        Cursor = next == ReminderButton.None ? Cursors.Default : Cursors.Hand;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoveredButton = ReminderButton.None;
        Cursor = Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;

        switch (HitTest(e.Location))
        {
            case ReminderButton.Acknowledge:
                Acknowledge();
                break;
            case ReminderButton.Confirm:
                Confirm();
                break;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        PaintBackdrop(g);
        PaintCard(g);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _countdownTimer.Dispose();
            _animationTimer.Dispose();
            _focusTimer.Dispose();
            _eyebrowFont.Dispose();
            _titleFont.Dispose();
            _bodyFont.Dispose();
            _buttonFont.Dispose();
            _metaFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void LayoutOverlay()
    {
        var width = Math.Max(560, ClientSize.Width - 28);
        var height = Math.Max(390, ClientSize.Height - 28);
        var x = (ClientSize.Width - width) / 2f;
        var y = (ClientSize.Height - height) / 2f;
        var eased = EaseOutCubic(_introProgress);
        var introOffset = (1f - eased) * 10f;

        _cardBounds = new RectangleF(x, y + introOffset, width, height);

        var buttonTop = _cardBounds.Bottom - 82;
        var buttonGap = 16;
        var buttonWidth = (_cardBounds.Width - 96 - buttonGap) / 2f;
        _acknowledgeBounds = new RectangleF(_cardBounds.Left + 48, buttonTop, buttonWidth, 50);
        _confirmBounds = new RectangleF(_acknowledgeBounds.Right + buttonGap, buttonTop, buttonWidth, 50);
    }

    private void PaintBackdrop(Graphics g)
    {
        using var baseBrush = new SolidBrush(Color.FromArgb(7, 8, 9));
        g.FillRectangle(baseBrush, ClientRectangle);

        using var radial = new LinearGradientBrush(
            ClientRectangle,
            Color.FromArgb(20, 33, 34),
            Color.FromArgb(3, 4, 5),
            LinearGradientMode.ForwardDiagonal);
        g.FillRectangle(radial, ClientRectangle);

        using var linePen = new Pen(Color.FromArgb(14, 245, 239, 226), 1);
        for (var x = -ClientSize.Height; x < ClientSize.Width; x += 42)
            g.DrawLine(linePen, x, ClientSize.Height, x + ClientSize.Height, 0);
    }

    private void PaintCard(Graphics g)
    {
        LayoutOverlay();

        using var path = RoundedRect(_cardBounds, 34);
        using var cardBrush = new LinearGradientBrush(
            _cardBounds,
            Color.FromArgb(252, 13, 15, 15),
            Color.FromArgb(252, 22, 24, 23),
            LinearGradientMode.Vertical);
        g.FillPath(cardBrush, path);

        using var borderPen = new Pen(Color.FromArgb(86, 239, 233, 218), 1.4f);
        g.DrawPath(borderPen, path);

        PaintContent(g);
    }

    private void PaintContent(Graphics g)
    {
        var pad = 48f;
        var left = _cardBounds.Left + pad;
        var top = _cardBounds.Top + 36;
        var right = _cardBounds.Right - pad;
        var contentWidth = right - left;
        var progress = _acknowledged ? 1f - Math.Clamp(_remainingSeconds / 10f, 0f, 1f) : 0f;

        DrawText(g, "REWARDS CHECK-IN", _eyebrowFont, Color.FromArgb(173, 230, 214), new RectangleF(left, top, contentWidth, 24), StringAlignment.Near);
        DrawText(g, "Bing searches are due.", _titleFont, Color.FromArgb(250, 247, 239), new RectangleF(left, top + 40, contentWidth, 48), StringAlignment.Near);
        DrawText(
            g,
            "This reminder stays on screen until you acknowledge it, wait ten seconds, and confirm that it can be cleared.",
            _bodyFont,
            Color.FromArgb(219, 211, 198),
            new RectangleF(left, top + 100, contentWidth - 20, 76),
            StringAlignment.Near);

        var statusText = _acknowledged
            ? _remainingSeconds > 0 ? $"Confirmation unlocks in {_remainingSeconds}s" : "Confirmation is ready"
            : "Action required before this overlay can close";
        DrawText(g, statusText, _metaFont, Color.FromArgb(186, 247, 223), new RectangleF(left, top + 190, contentWidth, 28), StringAlignment.Near);

        var rail = new RectangleF(left, top + 228, contentWidth, 7);
        using (var railPath = RoundedRect(rail, 4))
        using (var railBrush = new SolidBrush(Color.FromArgb(58, 255, 255, 255)))
            g.FillPath(railBrush, railPath);

        if (progress > 0)
        {
            var fill = new RectangleF(rail.Left, rail.Top, rail.Width * progress, rail.Height);
            using var fillPath = RoundedRect(fill, 4);
            using var fillBrush = new LinearGradientBrush(fill, Color.FromArgb(163, 255, 219), Color.FromArgb(116, 222, 255), LinearGradientMode.Horizontal);
            g.FillPath(fillBrush, fillPath);
        }

        PaintButton(g, _acknowledgeBounds, _acknowledged ? "ACKNOWLEDGED" : "ACKNOWLEDGE", ReminderButton.Acknowledge, !_acknowledged);
        PaintButton(g, _confirmBounds, _remainingSeconds > 0 ? $"CONFIRM IN {_remainingSeconds}s" : "CONFIRM", ReminderButton.Confirm, _acknowledged && _remainingSeconds <= 0, primary: true);
    }

    private void PaintButton(Graphics g, RectangleF bounds, string text, ReminderButton button, bool enabled, bool primary = false)
    {
        var hovered = enabled && _hoveredButton == button;
        var pulseOffset = primary && enabled ? (float)Math.Sin(_pulse) * 7 : 0;
        var top = enabled
            ? primary ? Color.FromArgb(156 + (int)pulseOffset, 255, 220) : Color.FromArgb(238, 235, 226)
            : Color.FromArgb(36, 38, 38);
        var bottom = enabled
            ? primary ? Color.FromArgb(105, 225, 252) : Color.FromArgb(210, 206, 196)
            : Color.FromArgb(30, 31, 31);
        var textColor = enabled
            ? Color.FromArgb(7, 10, 10)
            : Color.FromArgb(123, 124, 121);

        if (hovered)
            bounds.Inflate(0, 1);

        using var path = RoundedRect(bounds, 18);
        using var brush = new LinearGradientBrush(bounds, top, bottom, LinearGradientMode.Vertical);
        g.FillPath(brush, path);

        using var border = new Pen(enabled ? Color.FromArgb(220, 255, 252, 241) : Color.FromArgb(68, 89, 90, 86), 1.2f);
        g.DrawPath(border, path);

        DrawText(g, text, _buttonFont, textColor, bounds, StringAlignment.Center);
    }

    private void Acknowledge()
    {
        if (_acknowledged)
            return;

        _acknowledged = true;
        _remainingSeconds = 10;
        _countdownTimer.Start();
        KeepOverlayInFront();
        Invalidate();
    }

    private void TickCountdown()
    {
        _remainingSeconds--;
        if (_remainingSeconds <= 0)
        {
            _remainingSeconds = 0;
            _countdownTimer.Stop();
        }

        Invalidate();
    }

    private void TickAnimation()
    {
        _introProgress = Math.Min(1f, _introProgress + 0.055f);
        _pulse += 0.08f;
        Invalidate();
    }

    private void Confirm()
    {
        if (!_acknowledged || _remainingSeconds > 0)
            return;

        _allowClose = true;
        Close();
    }

    private ReminderButton HitTest(Point point)
    {
        if (!_acknowledged && _acknowledgeBounds.Contains(point))
            return ReminderButton.Acknowledge;

        if (_acknowledged && _remainingSeconds <= 0 && _confirmBounds.Contains(point))
            return ReminderButton.Confirm;

        return ReminderButton.None;
    }

    private void KeepOverlayInFront()
    {
        if (IsDisposed || _allowClose)
            return;

        TopMost = true;
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;

        SetWindowPos(Handle, HwndTopMost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
        BringToFront();
        Activate();
        Focus();
    }

    private void CenterOnCurrentScreen()
    {
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(
            area.Left + (area.Width - Width) / 2,
            area.Top + (area.Height - Height) / 2);
    }

    private void ApplyWindowShape()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;

        using var path = RoundedRect(new RectangleF(0, 0, ClientSize.Width, ClientSize.Height), 30);
        var nextRegion = new Region(path);
        var oldRegion = Region;
        Region = nextRegion;
        oldRegion?.Dispose();
    }

    private static bool IsBlockedShortcut(Keys keyData)
    {
        return keyData is Keys.Escape
            or (Keys.Alt | Keys.F4)
            or (Keys.Alt | Keys.Space)
            or (Keys.Control | Keys.F4);
    }

    private static float EaseOutCubic(float value)
    {
        var t = 1f - value;
        return 1f - t * t * t;
    }

    private static void DrawText(Graphics g, string text, Font font, Color color, RectangleF bounds, StringAlignment alignment)
    {
        using var brush = new SolidBrush(color);
        using var format = new StringFormat
        {
            Alignment = alignment,
            LineAlignment = alignment == StringAlignment.Center ? StringAlignment.Center : StringAlignment.Near,
            Trimming = StringTrimming.EllipsisWord
        };
        g.DrawString(text, font, brush, bounds, format);
    }

    private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Font CreateFont(string[] families, float size, FontStyle style)
    {
        using var installed = new InstalledFontCollection();
        var names = installed.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var family = families.FirstOrDefault(names.Contains) ?? FontFamily.GenericSansSerif.Name;
        return new Font(family, size, style, GraphicsUnit.Point);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    private enum ReminderButton
    {
        None,
        Acknowledge,
        Confirm
    }
}
