namespace WinTextCapture;

internal sealed class SettingsForm : Form
{
    private readonly List<OcrLanguageOption> _ocrOptions;
    private readonly TextBox _hotkeyBox;
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

    public SettingsForm(Settings settings)
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
        ClientSize = new Size(440, 362);

        var headFont = new Font("Segoe UI Semibold", 9f);
        int y = 18;

        Controls.Add(new Label { Text = Strings.HotkeyHead, AutoSize = true, Left = 18, Top = y, Font = headFont, Tag = "head" });
        y += 22;
        Controls.Add(new Label { Text = Strings.HotkeyHint, Left = 18, Top = y, Width = 400, Height = 36, Tag = "subtle" });
        y += 40;

        _hotkeyBox = new TextBox
        {
            Left = 18,
            Top = y,
            Width = 292,
            Height = 26,
            ReadOnly = true,
            Cursor = Cursors.Hand,
            TextAlign = HorizontalAlignment.Center,
            Font = new Font("Segoe UI Semibold", 9.5f),
            BorderStyle = BorderStyle.FixedSingle
        };
        _hotkeyBox.KeyDown += HotkeyBox_KeyDown;
        Controls.Add(_hotkeyBox);

        var clear = new Button { Text = Strings.Clear, Left = 320, Top = y - 1, Width = 100, Height = 28, FlatStyle = FlatStyle.Flat };
        clear.Click += (s, e) =>
        {
            _ctrl = _alt = _shift = _win = false;
            _key = Keys.None;
            UpdateHotkeyText();
        };
        Controls.Add(clear);
        y += 48;

        Controls.Add(new Label { Text = Strings.OcrHead, AutoSize = true, Left = 18, Top = y, Font = headFont, Tag = "head" });
        y += 26;
        Controls.Add(new Label { Text = Strings.OcrLangLabel, AutoSize = true, Left = 18, Top = y + 4, Tag = "subtle" });
        _ocrCombo = new ComboBox { Left = 172, Top = y, Width = 248, Height = 24, DropDownStyle = ComboBoxStyle.DropDownList };
        _ocrCombo.Items.AddRange(_ocrOptions.Cast<object>().ToArray());
        int ocrIndex = _ocrOptions.FindIndex(o => o.Tag.Equals(settings.OcrLanguage, StringComparison.OrdinalIgnoreCase));
        _ocrCombo.SelectedIndex = ocrIndex >= 0 ? ocrIndex : 0;
        Controls.Add(_ocrCombo);
        y += 46;

        Controls.Add(new Label { Text = Strings.GeneralHead, AutoSize = true, Left = 18, Top = y, Font = headFont, Tag = "head" });
        y += 26;
        _autoStart = new CheckBox { Text = Strings.StartWithWindows, Left = 18, Top = y, Width = 360, Height = 22, Checked = settings.AutoStart };
        Controls.Add(_autoStart);
        y += 46;

        Controls.Add(new Label { Text = Strings.AppearanceHead, AutoSize = true, Left = 18, Top = y, Font = headFont, Tag = "head" });
        y += 26;
        Controls.Add(new Label { Text = Strings.LanguageLabel, AutoSize = true, Left = 18, Top = y + 4, Tag = "subtle" });
        Controls.Add(new Label { Text = Strings.ThemeLabel, AutoSize = true, Left = 220, Top = y + 4, Tag = "subtle" });
        y += 22;

        _langCombo = new ComboBox { Left = 18, Top = y, Width = 180, Height = 24, DropDownStyle = ComboBoxStyle.DropDownList };
        _langCombo.Items.AddRange(new object[] { Strings.LangAuto, "English", "Türkçe" });
        _langCombo.SelectedIndex = settings.Language switch { "en" => 1, "tr" => 2, _ => 0 };
        Controls.Add(_langCombo);

        _themeCombo = new ComboBox { Left = 220, Top = y, Width = 200, Height = 24, DropDownStyle = ComboBoxStyle.DropDownList };
        _themeCombo.Items.AddRange(new object[] { Strings.ThemeSystem, Strings.ThemeLight, Strings.ThemeDark });
        _themeCombo.SelectedIndex = settings.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 };
        _themeCombo.SelectedIndexChanged += (s, e) => ApplyThemeLive();
        Controls.Add(_themeCombo);

        var cancel = new Button { Text = Strings.Cancel, Left = 324, Top = 316, Width = 96, Height = 30, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
        var ok = new Button { Text = Strings.Save, Left = 220, Top = 316, Width = 96, Height = 30, FlatStyle = FlatStyle.Flat, Tag = "primary", DialogResult = DialogResult.OK };
        ok.Click += Ok_Click;
        Controls.Add(ok);
        Controls.Add(cancel);

        AcceptButton = ok;
        CancelButton = cancel;
        UpdateHotkeyText();
        ApplyThemeLive();
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
        _hotkeyBox.Text = parts.Count == 0 ? Strings.HotkeyPressKeys : string.Join(" + ", parts);
    }

    private void Ok_Click(object? sender, EventArgs e)
    {
        if (_key == Keys.None || !(_ctrl || _alt || _shift || _win))
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
