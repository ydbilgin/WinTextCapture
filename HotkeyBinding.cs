namespace WinTextCapture;

internal readonly record struct HotkeyBinding(Keys Key, bool Ctrl, bool Alt, bool Shift, bool Win)
{
    public static HotkeyBinding FromSettings(Settings settings) =>
        new(settings.HotKey, settings.ModCtrl, settings.ModAlt, settings.ModShift, settings.ModWin);

    public bool IsValid => Key != Keys.None && (Ctrl || Alt || Shift || Win);

    public bool Matches(Keys key, bool ctrl, bool alt, bool shift, bool win) =>
        IsValid
        && key == Key
        && ctrl == Ctrl
        && alt == Alt
        && shift == Shift
        && win == Win;
}
