namespace WinTextCapture;

/// <summary>Owns tray UI, settings, hidden capture form, and global keyboard hook.</summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly BackgroundForm _background;
    private readonly KeyboardHook _hook;
    private readonly ToolStripMenuItem _captureItem;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _aboutItem;
    private readonly ToolStripMenuItem _exitItem;
    private readonly Icon _icon;
    private ToastForm? _toast;
    private Settings _settings;

    public TrayApplicationContext()
    {
        _settings = Settings.Load();
        Strings.Use(_settings.Language);
        _icon = AppIcon.Create();

        _captureItem = new ToolStripMenuItem("", null, (s, e) => CaptureNow());
        _settingsItem = new ToolStripMenuItem("", null, (s, e) => OpenSettings());
        _aboutItem = new ToolStripMenuItem("", null, (s, e) => ShowAbout());
        _exitItem = new ToolStripMenuItem("", null, (s, e) => ExitApp());

        var menu = new ContextMenuStrip();
        menu.Items.Add(_captureItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_settingsItem);
        menu.Items.Add(_aboutItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_exitItem);

        _tray = new NotifyIcon
        {
            Visible = true,
            ContextMenuStrip = menu,
            Icon = _icon,
        };
        _tray.DoubleClick += (s, e) => CaptureNow();

        _background = new BackgroundForm(ShowCaptureResult);
        _hook = new KeyboardHook(() => HotkeyBinding.FromSettings(_settings), CaptureNow);

        RebuildText();
        if (!_hook.Start())
            _tray.ShowBalloonTip(3000, App.Name, Strings.TipHotkeyFail(_settings.HotKeyText()), ToolTipIcon.Warning);
    }

    private void CaptureNow() => _background.StartCapture(_settings);

    private void OpenSettings()
    {
        using var form = new SettingsForm(_settings);
        form.Icon = _icon;
        if (form.ShowDialog() != DialogResult.OK)
            return;

        _settings = form.Result;
        _settings.Save();
        Strings.Use(_settings.Language);
        AutoStart.Apply(_settings.AutoStart);
        RebuildText();
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            Strings.AboutDialogBody(_settings.HotKeyText()),
            Strings.AboutTitle,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ShowCaptureResult(CaptureOutcome outcome)
    {
        string message = CaptureMessageFormatter.Format(outcome);

        if (!string.IsNullOrEmpty(message))
        {
            ShowToast(message);
            _tray.ShowBalloonTip(1600, App.Name, message, outcome.Status == CaptureStatus.Error ? ToolTipIcon.Warning : ToolTipIcon.Info);
        }
    }

    private void ShowToast(string message)
    {
        _toast?.Close();
        _toast = new ToastForm(message, Theming.Resolve(_settings.Theme));
        _toast.FormClosed += (s, e) =>
        {
            if (ReferenceEquals(_toast, s))
                _toast = null;
        };
        _toast.Show();
    }

    private void RebuildText()
    {
        string combo = _settings.HotKeyText();
        _tray.Text = Strings.TrayText(combo);
        _captureItem.Text = Strings.MenuCapture;
        _settingsItem.Text = Strings.MenuSettings;
        _aboutItem.Text = Strings.MenuAbout;
        _exitItem.Text = Strings.MenuExit;
    }

    private void ExitApp()
    {
        _tray.Visible = false;
        _hook.Dispose();
        _background.Dispose();
        _toast?.Close();
        _tray.Dispose();
        _icon.Dispose();
        ExitThread();
    }
}
