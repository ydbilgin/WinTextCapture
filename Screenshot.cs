using System.Drawing;
using System.Drawing.Imaging;

namespace WinTextCapture;

/// <summary>
/// Dev-only helper (behind the --screenshot arg): renders the settings window to a PNG without ever
/// showing it on a visible monitor — the form is placed far off the virtual desktop and captured via
/// DrawToBitmap (which renders the whole window, title bar included, even when off-screen).
/// </summary>
internal static class Screenshot
{
    public static void CaptureSettings(string path, string? themeOverride = null, string lang = "en")
    {
        var s = PublicSettings(themeOverride, lang);
        Strings.Use(s.Language);

        using var form = new SettingsForm(s, autoStartEnabled: false);
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(6000, 2000);   // beyond all monitors -> never visible
        form.ShowInTaskbar = false;
        form.Show();
        form.Activate();

        // Force every control to realize a handle + lay out, then settle, so DrawToBitmap captures the full UI.
        Realize(form);
        for (int i = 0; i < 6; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(40); }
        form.PerformLayout();
        form.Refresh();
        Application.DoEvents();

        int w = form.Width, h = form.Height;
        using (var bmp = new Bitmap(w, h))
        {
            form.DrawToBitmap(bmp, new Rectangle(0, 0, w, h));
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            bmp.Save(path, ImageFormat.Png);
        }

        form.Close();
    }

    /// <summary>The tray menu with its first item highlighted, drawn by the same renderer the tray uses.</summary>
    public static void CaptureTrayMenu(string path, string? themeOverride = null, string lang = "en")
    {
        var s = PublicSettings(themeOverride, lang);
        Strings.Use(s.Language);
        var menu = new TrayMenu(() => { }, () => { }, () => { }, () => { });
        menu.ApplyText(s);
        menu.ApplyTheme(Theming.Resolve(s.Theme));
        using var strip = menu.Strip;
        strip.Show(new Point(7000, 3000));
        menu.CaptureItem.Select();
        for (int i = 0; i < 4; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(30); }
        strip.Refresh();
        Save(strip, path);
        strip.Close();
    }

    private static void Save(Control control, string path)
    {
        using var bmp = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bmp, new Rectangle(0, 0, control.Width, control.Height));
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        bmp.Save(path, ImageFormat.Png);
    }

    /// <summary>
    /// Default settings (Win + Shift + T, automatic OCR language) with only the requested theme and UI
    /// language, so documentation pictures never show the local user's own preferences.
    /// </summary>
    internal static Settings PublicSettings(string? themeOverride, string? lang) => new()
    {
        Language = string.IsNullOrEmpty(lang) ? "en" : lang,
        Theme = string.IsNullOrEmpty(themeOverride) ? "System" : themeOverride,   // "Light" | "Dark"
    };

    private static void Realize(Control c)
    {
        _ = c.Handle;            // force handle creation
        foreach (Control child in c.Controls) Realize(child);
    }
}
