using System.Drawing.Drawing2D;

namespace WinTextCapture;

/// <summary>
/// Result card in the bottom-right corner: app icon, bold first line, optional detail line and a green
/// (copied) or amber (anything else) accent stripe. Never activates; closes after 2.2 s.
/// </summary>
internal sealed class ToastForm : Form
{
    internal const int DisplayMilliseconds = 2200;

    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly ToastContent _content;
    private readonly Theme _theme;
    private readonly Font _titleFont;
    private readonly Font _bodyFont;
    private readonly Bitmap _icon;
    private readonly float _scale;

    public ToastForm(ToastContent content, Theme theme)
    {
        _content = content;
        _theme = theme;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Font = new Font("Segoe UI", 9f);
        BackColor = theme.Surface;
        ForeColor = theme.Text;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

        _scale = DeviceDpi / 96f;
        _titleFont = new Font("Segoe UI Semibold", 10f);
        _bodyFont = new Font("Segoe UI", 9f);
        using (var icon = AppIcon.Create()) _icon = icon.ToBitmap();
        ClientSize = MeasureCard();

        _timer.Interval = DisplayMilliseconds;
        _timer.Tick += (s, e) => Close();
    }

    internal Color StripeColor => _content.Success ? _theme.Success : _theme.Warning;

    private int Px(int value) => (int)Math.Round(value * _scale);

    private int TextLeft => Px(4 + 16 + 32 + 12);

    private int TextWidthLimit => Px(300);

    private Size MeasureCard()
    {
        var flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        var title = TextRenderer.MeasureText(_content.Title, _titleFont, new Size(TextWidthLimit, 0), flags);
        var body = _content.Body.Length == 0
            ? Size.Empty
            : TextRenderer.MeasureText(_content.Body, _bodyFont, new Size(TextWidthLimit, 0), flags);
        int textWidth = Math.Min(TextWidthLimit, Math.Max(Px(180), Math.Max(title.Width, body.Width)));
        int textHeight = title.Height + (body.Height > 0 ? Px(4) + body.Height : 0);
        return new Size(TextLeft + textWidth + Px(18), Math.Max(Px(32), textHeight) + Px(16) * 2);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_NOACTIVATE = 0x08000000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Theming.DarkTitleBar(this, BackColor.R < 128);
        int corners = Native.DWMWCP_ROUND;   // Windows 11 rounds the card; Windows 10 keeps square corners
        Native.DwmSetWindowAttribute(Handle, Native.DWMWA_WINDOW_CORNER_PREFERENCE, ref corners, sizeof(int));
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(area.Right - Width - 18, area.Bottom - Height - 18);
        _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        PaintCard(e.Graphics);
    }

    /// <summary>Corner radius of the card outline, matching the Windows 11 window rounding.</summary>
    internal int CornerRadius => Px(8);

    /// <summary>Draws the card at (0, 0) over the square client area (Windows 11 rounds the window).</summary>
    internal void PaintCard(Graphics g)
    {
        var bounds = new Rectangle(Point.Empty, ClientSize);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        using var card = Theming.Rounded(bounds, CornerRadius);
        using (var fill = new SolidBrush(_theme.Surface))
            g.FillRectangle(fill, bounds);

        var clip = g.Clip;
        g.SetClip(card);
        using (var stripe = new SolidBrush(StripeColor))
            g.FillRectangle(stripe, 0, 0, Px(4), bounds.Height);
        g.Clip = clip;

        using (var border = new Pen(_theme.BorderStrong)) g.DrawPath(border, card);

        int pad = Px(16);
        g.DrawImage(_icon, new Rectangle(Px(4) + pad, pad, Px(32), Px(32)));

        var flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        int width = bounds.Width - TextLeft - Px(18);
        var title = TextRenderer.MeasureText(_content.Title, _titleFont, new Size(width, 0), flags);
        int textHeight = title.Height;
        Size body = Size.Empty;
        if (_content.Body.Length > 0)
        {
            body = TextRenderer.MeasureText(_content.Body, _bodyFont, new Size(width, 0), flags);
            textHeight += Px(4) + body.Height;
        }
        int top = (bounds.Height - textHeight) / 2;
        TextRenderer.DrawText(g, _content.Title, _titleFont, new Rectangle(TextLeft, top, width, title.Height), _theme.Text, flags);
        if (body.Height > 0)
            TextRenderer.DrawText(g, _content.Body, _bodyFont, new Rectangle(TextLeft, top + title.Height + Px(4), width, body.Height), _theme.Subtle, flags);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _titleFont.Dispose();
            _bodyFont.Dispose();
            _icon.Dispose();
        }
        base.Dispose(disposing);
    }
}
