namespace WinTextCapture;

/// <summary>Owns tray UI, settings, hidden capture form, and global keyboard hook.</summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly BackgroundForm _background;
    private readonly KeyboardHook _hook;
    private readonly TrayMenu _menu;
    private readonly Icon _icon;
    private ToastForm? _toast;
    private Settings _settings;

    public TrayApplicationContext()
    {
        _settings = Settings.Load();
        Strings.Use(_settings.Language);
        _icon = AppIcon.Create();

        _menu = new TrayMenu(CaptureNow, OpenSettings, ShowAbout, ExitApp);
        // "Follow system" can change while the app runs, so the theme is resolved each time the menu opens.
        _menu.Strip.Opening += (s, e) => _menu.ApplyTheme(Theming.Resolve(_settings.Theme));
        _menu.ApplyTheme(Theming.Resolve(_settings.Theme));

        _tray = new NotifyIcon
        {
            Visible = true,
            ContextMenuStrip = _menu.Strip,
            Icon = _icon,
        };
        _tray.DoubleClick += (s, e) => CaptureNow();

        _background = new BackgroundForm(ShowCaptureResult);
        _hook = new KeyboardHook(() => HotkeyBinding.FromSettings(_settings), CaptureNow);

        RebuildText();
        if (!_hook.Start())
            _tray.ShowBalloonTip(3000, App.Name, Strings.TipHotkeyFail(_settings.HotKeyText()), ToolTipIcon.Warning);

        TryApplyAutoStart(AutoStart.ReconcileStartup);
    }

    private void CaptureNow() => _background.StartCapture(_settings);

    private void OpenSettings()
    {
        bool autoStartEnabled;
        try
        {
            autoStartEnabled = AutoStart.IsEnabled();
        }
        catch (Exception ex)
        {
            CrashLogger.Write(ex, fatal: false);
            _tray.ShowBalloonTip(3000, App.Name, Strings.TipAutoStartFail, ToolTipIcon.Warning);
            return;
        }

        using var form = new SettingsForm(_settings, autoStartEnabled);
        form.Icon = _icon;
        if (form.ShowDialog() != DialogResult.OK)
            return;

        if (!form.Result.Save())
        {
            _tray.ShowBalloonTip(4000, App.Name, Strings.SettingsSaveFailed, ToolTipIcon.Warning);
            return;
        }
        _settings = form.Result;
        Strings.Use(_settings.Language);
        TryApplyAutoStart(() => AutoStart.ApplyUserChange(_settings.AutoStart));
        RebuildText();
    }

    private void TryApplyAutoStart(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            CrashLogger.Write(ex, fatal: false);
            _tray.ShowBalloonTip(3000, App.Name, Strings.TipAutoStartFail, ToolTipIcon.Warning);
        }
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
        _menu.ApplyText(_settings);
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
