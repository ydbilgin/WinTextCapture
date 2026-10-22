using System.Drawing.Drawing2D;

namespace WinTextCapture;

/// <summary>The tray context menu: same items and actions as before, drawn in the app theme.</summary>
internal sealed class TrayMenu
{
    public ContextMenuStrip Strip { get; }
    public ToolStripMenuItem CaptureItem { get; }
    public ToolStripMenuItem SettingsItem { get; }
    public ToolStripMenuItem AboutItem { get; }
    public ToolStripMenuItem ExitItem { get; }

    public TrayMenu(Action capture, Action settings, Action about, Action exit)
    {
        CaptureItem = Item((s, e) => capture());
        SettingsItem = Item((s, e) => settings());
        AboutItem = Item((s, e) => about());
        ExitItem = Item((s, e) => exit());

        Strip = new ContextMenuStrip
        {
            Font = new Font("Segoe UI", 9f),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Padding = new Padding(4, 4, 4, 4),
            MinimumSize = new Size(208, 0),
        };
        Strip.Items.Add(CaptureItem);
        Strip.Items.Add(new ToolStripSeparator());
        Strip.Items.Add(SettingsItem);
        Strip.Items.Add(AboutItem);
        Strip.Items.Add(new ToolStripSeparator());
        Strip.Items.Add(ExitItem);
        Strip.Opened += (s, e) => RoundCorners();
    }

    private static ToolStripMenuItem Item(EventHandler click) =>
        new("", null, click) { Padding = new Padding(4, 5, 4, 5) };

    /// <summary>Texts in the current language and the current hotkey on the right of "Capture now".</summary>
    public void ApplyText(Settings settings)
    {
        CaptureItem.Text = Strings.MenuCapture;
        CaptureItem.ShortcutKeyDisplayString = settings.HotKeyShortText();
        SettingsItem.Text = Strings.MenuSettings;
        AboutItem.Text = Strings.MenuAbout;
        ExitItem.Text = Strings.MenuExit;
    }

    public void ApplyTheme(Theme theme)
    {
        Strip.Renderer = new ThemedMenuRenderer(theme);
        Strip.BackColor = theme.Surface;
        Strip.ForeColor = theme.Text;
    }

    private void RoundCorners()
    {
        if (!Strip.IsHandleCreated) return;
        int preference = Native.DWMWCP_ROUNDSMALL;
        Native.DwmSetWindowAttribute(Strip.Handle, Native.DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
    }
}

internal readonly record struct MenuColors(Color Back, Color Text, Color Shortcut, Color Hover, Color Border, Color Separator, Color Disabled);

/// <summary>Flat menu renderer: theme surface, rounded hover, muted shortcut text, no grey icon gutter.</summary>
internal sealed class ThemedMenuRenderer : ToolStripRenderer
{
    public ThemedMenuRenderer(Theme theme)
    {
        Theme = theme;
        Colors = For(theme);
    }

    public Theme Theme { get; }
    public MenuColors Colors { get; }

    public static MenuColors For(Theme t) =>
        new(Back: t.Surface, Text: t.Text, Shortcut: t.Subtle, Hover: t.AccentWeak, Border: t.BorderStrong, Separator: t.Border, Disabled: t.Faint);

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Colors.Back);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Colors.Border);
        var r = e.AffectedBounds;
        e.Graphics.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Colors.Back);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected || !e.Item.Enabled) return;
        var r = new Rectangle(Point.Empty, e.Item.Size);
        r.Inflate(-2, -1);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Theming.Rounded(r, 4);
        using var brush = new SolidBrush(Colors.Hover);
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        bool shortcut = e.Item is ToolStripMenuItem item
            && !string.IsNullOrEmpty(item.ShortcutKeyDisplayString)
            && e.Text == item.ShortcutKeyDisplayString;
        e.TextColor = !e.Item.Enabled ? Colors.Disabled : shortcut ? Colors.Shortcut : Colors.Text;
        var r = e.TextRectangle;
        if (shortcut)
        {
            // Right-align the hotkey 12 px from the item edge, however wide the menu is.
            e.TextRectangle = new Rectangle(r.X, r.Y, Math.Max(1, e.Item.Width - 12 - r.X), r.Height);
            e.TextFormat = (e.TextFormat & ~TextFormatFlags.HorizontalCenter) | TextFormatFlags.Right;
        }
        else
        {
            e.TextRectangle = new Rectangle(r.X + 8, r.Y, r.Width, r.Height);   // inner padding without an icon gutter
        }
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        int y = e.Item.Height / 2;
        using var pen = new Pen(Colors.Separator);
        e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = Colors.Text;
        base.OnRenderArrow(e);
    }
}
