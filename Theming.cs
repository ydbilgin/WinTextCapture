using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace WinTextCapture;

internal readonly record struct Theme(
    Color Back, Color Surface, Color Surface2, Color Text, Color Subtle, Color Faint,
    Color Accent, Color AccentWeak, Color Border, Color BorderStrong,
    Color Chip, Color ChipText, Color FocusRing, Color DisabledSurface)
{
    public bool IsDark => Back.R < 128;

    /// <summary>Text drawn on an accent fill (primary button, size badge).</summary>
    public Color AccentText => IsDark ? Color.FromArgb(23, 27, 33) : Color.FromArgb(253, 253, 254);

    public static readonly Theme Light = new(
        Back: Color.FromArgb(246, 247, 249), Surface: Color.FromArgb(253, 253, 254),
        Surface2: Color.FromArgb(254, 254, 255), Text: Color.FromArgb(32, 37, 45),
        Subtle: Color.FromArgb(95, 105, 119), Faint: Color.FromArgb(132, 142, 156),
        Accent: Color.FromArgb(53, 111, 214), AccentWeak: Color.FromArgb(231, 238, 251),
        Border: Color.FromArgb(210, 216, 224), BorderStrong: Color.FromArgb(183, 192, 203),
        Chip: Color.FromArgb(232, 236, 242), ChipText: Color.FromArgb(45, 52, 63),
        FocusRing: Color.FromArgb(169, 196, 242), DisabledSurface: Color.FromArgb(238, 241, 245));

    public static readonly Theme Dark = new(
        Back: Color.FromArgb(23, 27, 33), Surface: Color.FromArgb(29, 34, 42),
        Surface2: Color.FromArgb(35, 42, 52), Text: Color.FromArgb(232, 235, 240),
        Subtle: Color.FromArgb(173, 181, 194), Faint: Color.FromArgb(125, 136, 151),
        Accent: Color.FromArgb(116, 160, 242), AccentWeak: Color.FromArgb(37, 51, 74),
        Border: Color.FromArgb(54, 63, 75), BorderStrong: Color.FromArgb(81, 93, 108),
        Chip: Color.FromArgb(48, 57, 70), ChipText: Color.FromArgb(222, 227, 235),
        FocusRing: Color.FromArgb(63, 93, 137), DisabledSurface: Color.FromArgb(25, 30, 37));

    public static readonly Theme HighContrast = new(
        Back: SystemColors.Window, Surface: SystemColors.Window, Surface2: SystemColors.Window,
        Text: SystemColors.WindowText, Subtle: SystemColors.WindowText, Faint: SystemColors.GrayText,
        Accent: SystemColors.Highlight, AccentWeak: SystemColors.Window,
        Border: SystemColors.WindowText, BorderStrong: SystemColors.WindowText,
        Chip: SystemColors.Control, ChipText: SystemColors.ControlText,
        FocusRing: SystemColors.Highlight, DisabledSurface: SystemColors.Control);
}

internal enum ThemedButtonRole { Primary, Secondary }

/// <summary>Light/dark theming with small owner-drawn controls (no third-party UI library).</summary>
internal static class Theming
{
    public static Theme Resolve(string pref) => SystemInformation.HighContrast ? Theme.HighContrast : pref switch
    {
        "Dark" => Theme.Dark,
        "Light" => Theme.Light,
        _ => IsSystemDark() ? Theme.Dark : Theme.Light,
    };

    public static bool IsSystemDark()
    {
        try
        {
            using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return k?.GetValue("AppsUseLightTheme") is int v && v == 0;   // 0 = dark
        }
        catch { return false; }
    }

    public static void Apply(Control root, Theme t)
    {
        ApplyRec(root, t);
        if (root is Form f) DarkTitleBar(f, t.IsDark);
    }

    private static void ApplyRec(Control c, Theme t)
    {
        c.BackColor = t.Back;
        c.ForeColor = t.Text;

        switch (c)
        {
            case ThemedButton b: b.SetTheme(t); break;
            case ThemedCheckBox cb: cb.SetTheme(t); break;
            case ThemedComboBox combo: combo.SetTheme(t); break;
            case HotkeyField field: field.SetTheme(t); break;
            case Label lbl:
                lbl.ForeColor = (lbl.Tag as string) switch
                {
                    "subtle" => t.Subtle,
                    "faint" => t.Faint,
                    _ => t.Text,
                };
                break;
        }

        foreach (Control child in c.Controls)
            ApplyRec(child, t);
    }

    public static void DarkTitleBar(Form f, bool dark)
    {
        if (!f.IsHandleCreated) return;
        int v = dark ? 1 : 0;
        Native.DwmSetWindowAttribute(f.Handle, Native.DWMWA_USE_IMMERSIVE_DARK_MODE, ref v, sizeof(int));
    }

    public static GraphicsPath Rounded(Rectangle rect, int radius)
    {
        rect.Width = Math.Max(1, rect.Width - 1);
        rect.Height = Math.Max(1, rect.Height - 1);
        int d = Math.Max(2, Math.Min(radius * 2, Math.Min(rect.Width, rect.Height)));
        var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class ThemedButton : Button
{
    private Theme _theme = Theme.Light;
    private bool _hovered;
    private bool _pressed;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ThemedButtonRole Role { get; set; } = ThemedButtonRole.Secondary;

    public ThemedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Height = 32;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    public void SetTheme(Theme theme) { _theme = theme; Invalidate(); }
    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    /// <summary>Fill colour for the current state; exposed so tests can check it follows the theme.</summary>
    internal Color FillColor
    {
        get
        {
            bool primary = Role == ThemedButtonRole.Primary;
            if (!Enabled) return _theme.DisabledSurface;
            if (_pressed) return primary ? ControlPaint.Dark(_theme.Accent, .08f) : _theme.AccentWeak;
            if (_hovered) return primary ? ControlPaint.Light(_theme.Accent, .08f) : _theme.AccentWeak;
            return primary ? _theme.Accent : _theme.Surface2;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? _theme.Back);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        bool primary = Role == ThemedButtonRole.Primary;
        Color text = !Enabled ? _theme.Faint : primary ? _theme.AccentText : _theme.Text;
        using var path = Theming.Rounded(ClientRectangle, 5);
        using (var brush = new SolidBrush(FillColor)) e.Graphics.FillPath(brush, path);
        if (!primary) using (var pen = new Pen(_theme.BorderStrong)) e.Graphics.DrawPath(pen, path);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if (Focused && ShowFocusCues)
        {
            var focus = ClientRectangle;
            focus.Inflate(-2, -2);
            using var pen = new Pen(_theme.FocusRing, 2);
            using var focusPath = Theming.Rounded(focus, 4);
            e.Graphics.DrawPath(pen, focusPath);
        }
    }
}

internal sealed class ThemedCheckBox : CheckBox
{
    private Theme _theme = Theme.Light;
    private bool _hovered;

    public ThemedCheckBox()
    {
        AutoSize = false;
        Height = 32;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    public void SetTheme(Theme theme) { _theme = theme; Invalidate(); }
    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnCheckedChanged(EventArgs e) { Invalidate(); base.OnCheckedChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? _theme.Back);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int box = Math.Max(18, (int)Math.Round(18 * DeviceDpi / 96f));
        int x = 1, y = (Height - box) / 2;
        var rect = new Rectangle(x, y, box, box);
        Color fill = !Enabled ? _theme.DisabledSurface : Checked ? _theme.Accent : _theme.Surface2;
        Color border = Checked ? _theme.Accent : Focused || _hovered ? _theme.Subtle : _theme.BorderStrong;
        using (var path = Theming.Rounded(rect, 4))
        {
            using (var brush = new SolidBrush(fill)) e.Graphics.FillPath(brush, path);
            using (var pen = new Pen(border)) e.Graphics.DrawPath(pen, path);
        }
        if (Checked)
        {
            using var pen = new Pen(_theme.AccentText, Math.Max(2, box / 8f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            e.Graphics.DrawLines(pen, new[]
            {
                new Point(x + box / 4, y + box / 2),
                new Point(x + box * 7 / 16, y + box * 11 / 16),
                new Point(x + box * 3 / 4, y + box / 3),
            });
        }
        if (Focused && ShowFocusCues)
        {
            var ring = rect;
            ring.Inflate(2, 2);
            using var pen = new Pen(_theme.FocusRing, 2);
            using var ringPath = Theming.Rounded(ring, 5);
            e.Graphics.DrawPath(pen, ringPath);
        }
        var textRect = new Rectangle(rect.Right + 8, 0, Math.Max(1, Width - rect.Right - 8), Height);
        TextRenderer.DrawText(e.Graphics, Text, Font, textRect, Enabled ? _theme.Text : _theme.Faint,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordEllipsis | TextFormatFlags.NoPrefix);
    }
}

/// <summary>Drop-down list drawn in theme colours; a click-through overlay paints the closed box.</summary>
internal sealed class ThemedComboBox : ComboBox
{
    private Theme _theme = Theme.Light;
    private bool _hovered;
    private ComboOverlay? _overlay;

    public ThemedComboBox()
    {
        DropDownStyle = ComboBoxStyle.DropDownList;
        FlatStyle = FlatStyle.Flat;
        DrawMode = DrawMode.OwnerDrawFixed;
        ItemHeight = 26;
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
    }

    public void SetTheme(Theme theme)
    {
        _theme = theme;
        BackColor = theme.Surface2;
        ForeColor = theme.Text;
        _overlay?.SetTheme(theme, Focused || _hovered);
        Invalidate();
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        bool selected = (e.State & DrawItemState.Selected) != 0;
        using var fill = new SolidBrush(selected ? _theme.AccentWeak : _theme.Surface2);
        e.Graphics.FillRectangle(fill, e.Bounds);
        TextRenderer.DrawText(e.Graphics, GetItemText(Items[e.Index]), Font, Rectangle.Inflate(e.Bounds, -8, 0), _theme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _overlay ??= new ComboOverlay(this);
        if (!Controls.Contains(_overlay)) Controls.Add(_overlay);
        LayoutOverlay();
        _overlay.SetTheme(_theme, Focused || _hovered);
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); LayoutOverlay(); }
    protected override void OnSelectedIndexChanged(EventArgs e) { base.OnSelectedIndexChanged(e); _overlay?.Invalidate(); }
    protected override void OnMouseEnter(EventArgs e) { _hovered = true; UpdateOverlay(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; UpdateOverlay(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { UpdateOverlay(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { UpdateOverlay(); base.OnLostFocus(e); }
    private void UpdateOverlay() { Invalidate(); _overlay?.SetTheme(_theme, Focused || _hovered); }

    private void LayoutOverlay()
    {
        if (_overlay == null || Width <= 2 || Height <= 2) return;
        _overlay.Bounds = ClientRectangle;
        _overlay.BringToFront();
    }

    private sealed class ComboOverlay : Control
    {
        private const int WmNcHitTest = 0x0084, HtTransparent = -1;
        private readonly ThemedComboBox _owner;
        private Theme _theme = Theme.Light;
        private bool _strong;

        public ComboOverlay(ThemedComboBox owner)
        {
            _owner = owner;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        public void SetTheme(Theme theme, bool strong) { _theme = theme; _strong = strong; Invalidate(); }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmNcHitTest) { m.Result = (IntPtr)HtTransparent; return; }
            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(_owner.Parent?.BackColor ?? _theme.Back);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = Theming.Rounded(ClientRectangle, 5))
            {
                using (var fill = new SolidBrush(_theme.Surface2)) g.FillPath(fill, path);
                using (var border = new Pen(_strong ? _theme.Accent : _theme.BorderStrong)) g.DrawPath(border, path);
            }
            int arrowWidth = Math.Min(32, Math.Max(24, Height));
            string selected = _owner.SelectedIndex >= 0 ? _owner.GetItemText(_owner.SelectedItem) ?? "" : "";
            TextRenderer.DrawText(g, selected, _owner.Font, new Rectangle(10, 1, Math.Max(0, Width - arrowWidth - 12), Height - 2),
                _theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            int cx = Width - arrowWidth / 2, cy = Height / 2;
            using var glyph = new Pen(_theme.Subtle, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLines(glyph, new[] { new Point(cx - 4, cy - 2), new Point(cx, cy + 2), new Point(cx + 4, cy - 2) });
        }
    }
}

/// <summary>
/// Focusable "click, then press keys" field. It shows the combo as key chips, never a text selection,
/// and raises the normal KeyDown event for every key a read-only text box used to receive.
/// </summary>
internal sealed class HotkeyField : Control
{
    private Theme _theme = Theme.Light;
    private bool _hovered;
    private IReadOnlyList<string> _parts = Array.Empty<string>();

    public HotkeyField()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.Selectable | ControlStyles.ResizeRedraw, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        Height = 32;
    }

    /// <summary>Key names shown as chips, for example ["Shift", "Win", "T"]. Empty shows the placeholder.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<string> Parts
    {
        get => _parts;
        set { _parts = value ?? Array.Empty<string>(); Invalidate(); }
    }

    /// <summary>Shown when no key is set.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Placeholder { get; set; } = "";

    /// <summary>Shown on the right while the field has focus.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ListeningText { get; set; } = "";

    public void SetTheme(Theme theme) { _theme = theme; Invalidate(); }

    // Same routing as the read-only TextBox it replaces: Tab, Enter and Esc stay dialog keys,
    // Alt combinations go through the normal system-key path, everything else reaches KeyDown.
    protected override bool IsInputKey(Keys keyData)
    {
        if ((keyData & Keys.Alt) == Keys.Alt) return false;
        return (keyData & Keys.KeyCode) switch
        {
            Keys.Tab or Keys.Enter or Keys.Escape => false,
            _ => true,
        };
    }

    protected override void OnMouseDown(MouseEventArgs e) { Focus(); base.OnMouseDown(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? _theme.Back);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = Theming.Rounded(ClientRectangle, 5))
        {
            using (var fill = new SolidBrush(_theme.Surface2)) g.FillPath(fill, path);
            using (var pen = new Pen(Focused ? _theme.Accent : _hovered ? _theme.Subtle : _theme.BorderStrong, Focused ? 2f : 1f))
                g.DrawPath(pen, path);
        }

        int x = 8;
        int right = Width - 8;
        if (Focused && ListeningText.Length > 0)
        {
            var size = TextRenderer.MeasureText(g, ListeningText, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            right -= size.Width;
            TextRenderer.DrawText(g, ListeningText, Font, new Rectangle(right, 0, size.Width, Height), _theme.Accent,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            right -= 8;
        }

        if (_parts.Count == 0)
        {
            if (Focused && ListeningText.Length > 0) return;   // "Press keys" on the right says it already
            TextRenderer.DrawText(g, Placeholder, Font, new Rectangle(x + 2, 0, Math.Max(1, right - x), Height), _theme.Faint,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            return;
        }

        int chipHeight = Math.Max(16, Height - 12);
        int chipTop = (Height - chipHeight) / 2;
        using var chipFill = new SolidBrush(_theme.Chip);
        using var chipBorder = new Pen(_theme.Border);
        for (int i = 0; i < _parts.Count; i++)
        {
            if (i > 0)
            {
                var plus = TextRenderer.MeasureText(g, "+", Font, Size.Empty, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, "+", Font, new Rectangle(x, 0, plus.Width, Height), _theme.Faint,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                x += plus.Width + 4;
            }
            var text = TextRenderer.MeasureText(g, _parts[i], Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            var chip = new Rectangle(x, chipTop, text.Width + 14, chipHeight);
            if (chip.Right > right) break;
            using (var chipPath = Theming.Rounded(chip, 4))
            {
                g.FillPath(chipFill, chipPath);
                g.DrawPath(chipBorder, chipPath);
            }
            TextRenderer.DrawText(g, _parts[i], Font, chip, _theme.ChipText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            x = chip.Right + 4;
        }
    }
}
