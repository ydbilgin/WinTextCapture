namespace WinTextCapture;

internal sealed class SettingsForm : Form
{
    private readonly List<OcrLanguageOption> _ocrOptions;
    private readonly HotkeyField _hotkeyBox;
    private readonly ComboBox _ocrCombo;
    private readonly CheckBox _autoStart;
    private readonly ComboBox _langCombo;
    private readonly ComboBox _themeCombo;
    private bool _ctrl;
    private bool _alt;
    private bool _shift;
    private bool _win;
    private Keys _key;

    public Settings Result { get; private set; }

    public SettingsForm(Settings settings, bool autoStartEnabled)
    {
        Result = settings;
        _ctrl = settings.ModCtrl;
        _alt = settings.ModAlt;
        _shift = settings.ModShift;
        _win = settings.ModWin;
        _key = settings.HotKey;
        _ocrOptions = OcrLanguageOption.Installed();

        Text = Strings.SettingsTitle;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        // Layout on an 8 px grid: 24 px margins, a 24 px header slot, 8 px from header to its row,
        // 24 px between sections. Same fields and order as before.
        const int Margin = 24, ContentWidth = 416, RowHeight = 32;
        var headFont = new Font("Segoe UI Semibold", 10f);
        int y = Margin;

        Controls.Add(Head(Strings.HotkeyHead, y, headFont));
        y += 24;
        Controls.Add(new Label { Text = Strings.HotkeyHint, Left = Margin, Top = y, Width = ContentWidth, Height = 40, Tag = "subtle" });
        y += 40;

        _hotkeyBox = new HotkeyField
        {
            Left = Margin,
            Top = y,
            Width = 304,
            Height = RowHeight,
            Font = new Font("Segoe UI Semibold", 9f),
            Placeholder = Strings.HotkeyPressKeys,
            ListeningText = Strings.HotkeyListening,
            AccessibleName = Strings.HotkeyHead,
        };
        _hotkeyBox.KeyDown += HotkeyBox_KeyDown;
        Controls.Add(_hotkeyBox);

        var clear = new ThemedButton { Text = Strings.Clear, Left = Margin + 304 + 8, Top = y, Width = 104, Height = RowHeight };
        clear.Click += (s, e) =>
        {
            _ctrl = _alt = _shift = _win = false;
            _key = Keys.None;
            UpdateHotkeyText();
        };
        Controls.Add(clear);
        y += RowHeight + 24;

        Controls.Add(Head(Strings.OcrHead, y, headFont));
        y += 32;
        Controls.Add(RowLabel(Strings.OcrLangLabel, Margin, y, 168));
        _ocrCombo = new ThemedComboBox { Left = 200, Top = y, Width = 240 };
        _ocrCombo.Items.AddRange(_ocrOptions.Cast<object>().ToArray());
        int ocrIndex = _ocrOptions.FindIndex(o => o.Tag.Equals(settings.OcrLanguage, StringComparison.OrdinalIgnoreCase));
        _ocrCombo.SelectedIndex = ocrIndex >= 0 ? ocrIndex : 0;
        Controls.Add(_ocrCombo);
        y += RowHeight + 24;

        Controls.Add(Head(Strings.GeneralHead, y, headFont));
        y += 32;
        _autoStart = new ThemedCheckBox { Text = Strings.StartWithWindows, Left = Margin, Top = y, Width = ContentWidth, Height = RowHeight, Checked = autoStartEnabled };
        Controls.Add(_autoStart);
        y += RowHeight + 24;

        Controls.Add(Head(Strings.AppearanceHead, y, headFont));
        y += 32;
        Controls.Add(new Label { Text = Strings.LanguageLabel, AutoSize = false, Left = Margin, Top = y, Width = 200, Height = 20, Tag = "subtle" });
        Controls.Add(new Label { Text = Strings.ThemeLabel, AutoSize = false, Left = 240, Top = y, Width = 200, Height = 20, Tag = "subtle" });
        y += 24;

        _langCombo = new ThemedComboBox { Left = Margin, Top = y, Width = 200 };
        _langCombo.Items.AddRange(new object[] { Strings.LangAuto, "English", "Türkçe" });
        _langCombo.SelectedIndex = settings.Language switch { "en" => 1, "tr" => 2, _ => 0 };
        Controls.Add(_langCombo);

        _themeCombo = new ThemedComboBox { Left = 240, Top = y, Width = 200 };
        _themeCombo.Items.AddRange(new object[] { Strings.ThemeSystem, Strings.ThemeLight, Strings.ThemeDark });
        _themeCombo.SelectedIndex = settings.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 };
        _themeCombo.SelectedIndexChanged += (s, e) => ApplyThemeLive();
        Controls.Add(_themeCombo);
        y += RowHeight + 24;

        _footerTop = y;
        y += 16;
        var ok = new ThemedButton { Text = Strings.Save, Left = 240, Top = y, Width = 96, Height = RowHeight, Role = ThemedButtonRole.Primary, DialogResult = DialogResult.OK };
        var cancel = new ThemedButton { Text = Strings.Cancel, Left = 344, Top = y, Width = 96, Height = RowHeight, DialogResult = DialogResult.Cancel };
        ok.Click += Ok_Click;
        Controls.Add(ok);
        Controls.Add(cancel);
        y += RowHeight + Margin;
        ClientSize = new Size(Margin * 2 + ContentWidth, y);

        AcceptButton = ok;
        CancelButton = cancel;
        UpdateHotkeyText();
        ApplyThemeLive();
    }

    private readonly int _footerTop;

    private static Label Head(string text, int top, Font font) =>
        new() { Text = text, AutoSize = false, Left = 24, Top = top, Width = 416, Height = 24, Font = font, TextAlign = ContentAlignment.MiddleLeft };

    private static Label RowLabel(string text, int left, int top, int width) =>
        new() { Text = text, AutoSize = false, Left = left, Top = top, Width = width, Height = 32, TextAlign = ContentAlignment.MiddleLeft, Tag = "subtle" };

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var rule = new Pen(SelectedTheme().Border);
        e.Graphics.DrawLine(rule, 0, _footerTop, ClientSize.Width, _footerTop);
    }

    private void HotkeyBox_KeyDown(object? sender, KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        e.Handled = true;

        _ctrl = e.Control;
        _alt = e.Alt;
        _shift = e.Shift;
        _win = Native.IsDown(Native.VK_LWIN) || Native.IsDown(Native.VK_RWIN) || e.KeyCode is Keys.LWin or Keys.RWin;

        if (e.KeyCode is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey or Keys.Menu or Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey)
        {
            _key = Keys.None;
            UpdateHotkeyText();
            return;
        }

        if (e.KeyCode is Keys.LWin or Keys.RWin)
        {
            _key = Keys.None;
            UpdateHotkeyText();
            return;
        }

        _key = e.KeyCode;
        UpdateHotkeyText();
    }

    private void UpdateHotkeyText()
    {
        var parts = new List<string>();
        if (_ctrl) parts.Add("Ctrl");
        if (_alt) parts.Add("Alt");
        if (_shift) parts.Add("Shift");
        if (_win) parts.Add("Win");
        if (_key != Keys.None) parts.Add(_key.ToString());
        _hotkeyBox.Parts = parts;
        _hotkeyBox.Text = parts.Count == 0 ? Strings.HotkeyPressKeys : string.Join(" + ", parts);
    }

    private void Ok_Click(object? sender, EventArgs e)
    {
        if (!new HotkeyBinding(_key, _ctrl, _alt, _shift, _win).IsValid)
        {
            MessageBox.Show(Strings.ValidationPickKey, App.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        Result = new Settings
        {
            ModCtrl = _ctrl,
            ModAlt = _alt,
            ModShift = _shift,
            ModWin = _win,
            HotKey = _key,
            OcrLanguage = SelectedOcrLanguage(),
            AutoStart = _autoStart.Checked,
            Language = _langCombo.SelectedIndex switch { 1 => "en", 2 => "tr", _ => "auto" },
            Theme = _themeCombo.SelectedIndex switch { 1 => "Light", 2 => "Dark", _ => "System" },
        };
    }

    private string SelectedOcrLanguage() =>
        _ocrCombo.SelectedItem is OcrLanguageOption option ? option.Tag : "auto";

    private Theme SelectedTheme() => Theming.Resolve(_themeCombo.SelectedIndex switch { 1 => "Light", 2 => "Dark", _ => "System" });

    private void ApplyThemeLive()
    {
        Theming.Apply(this, SelectedTheme());
        Invalidate(true);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Theming.DarkTitleBar(this, SelectedTheme().IsDark);
    }
}
