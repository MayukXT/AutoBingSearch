namespace AutoBingSearch.App.Forms;

internal static class AppTheme
{
    public static readonly Color Background = Color.FromArgb(248, 247, 244);
    public static readonly Color Panel = Color.White;
    public static readonly Color Text = Color.FromArgb(28, 28, 30);
    public static readonly Color Muted = Color.FromArgb(102, 102, 110);
    public static readonly Color Border = Color.FromArgb(218, 216, 210);
    public static readonly Color Primary = Color.FromArgb(20, 20, 20);
    public static readonly Color PrimaryText = Color.White;
    public static readonly Color Disabled = Color.FromArgb(226, 224, 218);
    public static readonly Font TitleFont = new("Bahnschrift", 18, FontStyle.Bold);
    public static readonly Font BodyFont = new("Candara", 10.5f, FontStyle.Regular);
    public static readonly Font ButtonFont = new("Bahnschrift", 10.5f, FontStyle.Bold);

    public static Icon LoadAppIcon()
    {
        return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
    }

    public static Button Button(string text, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            Font = ButtonFont,
            BackColor = primary ? Primary : Panel,
            ForeColor = primary ? PrimaryText : Text,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = primary ? Primary : Border;
        return button;
    }

    public static void SetButtonState(Button button, bool enabled, bool primary = false)
    {
        button.Enabled = enabled;
        button.BackColor = enabled
            ? primary ? Primary : Panel
            : Disabled;
        button.ForeColor = enabled
            ? primary ? PrimaryText : Text
            : Muted;
        button.FlatAppearance.BorderColor = enabled
            ? primary ? Primary : Border
            : Border;
    }
}
