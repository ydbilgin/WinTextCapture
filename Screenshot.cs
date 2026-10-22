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

    /// <summary>
    /// The capture overlay over a synthetic document (never a screen grab), with a selection drawn the
    /// way a drag draws it. The overlay window is never shown; its paint routine renders into a bitmap.
    /// </summary>
    public static void CaptureOverlay(string path, string? themeOverride = null, string lang = "en")
    {
        var s = PublicSettings(themeOverride, lang);
        Strings.Use(s.Language);
        using var background = SyntheticDesktop(s.Language == "tr");
        using var overlay = new OverlayForm(s, background, _ => Task.FromResult(CaptureOutcome.Cancelled));
        var client = new Rectangle(Point.Empty, background.Size);
        overlay.HintArea = client;
        overlay.PreviewSelection(new Rectangle(196, 234, 560, 108));
        // 24-bit targets: GDI text drawn on 32-bit bitmaps loses its alpha and prints as dark fringes.
        using var bmp = new Bitmap(background.Width, background.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
            overlay.PaintOverlay(g, client);
        SavePng(bmp, path);
    }

    private static Bitmap SyntheticDesktop(bool turkish)
    {
        var bmp = new Bitmap(960, 540, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(38, 70, 110));
        var page = new Rectangle(160, 96, 640, 400);
        using (var shadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0))) g.FillRectangle(shadow, page with { X = page.X + 4, Y = page.Y + 6 });
        g.FillRectangle(Brushes.White, page);
        using (var bar = new SolidBrush(Color.FromArgb(241, 243, 246))) g.FillRectangle(bar, page with { Height = 36 });
        using var titleFont = new Font("Segoe UI", 10f);
        using var headFont = new Font("Segoe UI Semibold", 15f);
        using var bodyFont = new Font("Segoe UI", 11f);
        TextRenderer.DrawText(g, turkish ? "notlar.txt - Not Defteri" : "notes.txt - Notepad", titleFont, new Point(page.X + 14, page.Y + 9), Color.FromArgb(60, 64, 70));
        TextRenderer.DrawText(g, turkish ? "Toplantı notları" : "Meeting notes", headFont, new Point(page.X + 36, page.Y + 64), Color.FromArgb(25, 28, 33));
        string[] lines = turkish
            ? new[] { "Fatura numarası: TR-2026-0418", "Teslim tarihi: 14 Kasım, sabah 10:30", "Adres: Atatürk Cad. No 12, İzmir" }
            : new[] { "Invoice number: INV-2026-0418", "Delivery date: 14 November, 10:30 am", "Address: 12 Harbour Street, Portsmouth" };
        int y = page.Y + 148;
        foreach (string line in lines)
        {
            TextRenderer.DrawText(g, line, bodyFont, new Point(page.X + 44, y), Color.FromArgb(40, 44, 52));
            y += 36;
        }
        return bmp;
    }

    private static void SavePng(Bitmap bmp, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        bmp.Save(path, ImageFormat.Png);
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
