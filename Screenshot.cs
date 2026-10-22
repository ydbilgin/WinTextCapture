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
    /// way a drag draws it. The overlay window is never shown: DrawToBitmap runs its own paint routine
    /// into a screen-compatible bitmap, the same path the settings picture uses.
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
        using var bmp = new Bitmap(client.Width, client.Height);
        overlay.DrawToBitmap(bmp, client);
        SavePng(bmp, path);
    }

    /// <summary>
    /// The result toast for a synthetic "copied" or "notext" outcome on a plain backdrop. The toast is
    /// never shown: DrawToBitmap runs its paint routine, then the card is cut to its rounded outline
    /// (Windows 11 rounds the real window the same way).
    /// </summary>
    public static void CaptureToast(string path, string? themeOverride, string lang, string? variant)
    {
        var s = PublicSettings(themeOverride, lang);
        Strings.Use(s.Language);
        var theme = Theming.Resolve(s.Theme);
        var outcome = variant == "notext"
            ? CaptureOutcome.NoText
            : CaptureOutcome.Copied(s.Language == "tr"
                ? "Fatura numarası: TR-2026-0418\nTeslim tarihi: 14 Kasım, sabah 10:30"
                : "Invoice number: INV-2026-0418\nDelivery date: 14 November, 10:30 am");
        var content = CaptureMessageFormatter.Toast(outcome)!;
        using var toast = new ToastForm(content, theme);
        var size = toast.ClientSize;
        using var card = new Bitmap(size.Width, size.Height);
        toast.DrawToBitmap(card, new Rectangle(Point.Empty, size));

        const int margin = 24;
        using var bmp = new Bitmap(size.Width + margin * 2, size.Height + margin * 2);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(theme.IsDark ? Color.FromArgb(52, 60, 72) : Color.FromArgb(214, 220, 228));
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var outline = Theming.Rounded(new Rectangle(new Point(margin, margin), size), toast.CornerRadius);
            using var brush = new TextureBrush(card);
            brush.TranslateTransform(margin, margin);
            g.FillPath(brush, outline);
        }
        SavePng(bmp, path);
    }

    private static Bitmap SyntheticDesktop(bool turkish)
    {
        var bmp = new Bitmap(960, 540);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.FromArgb(38, 70, 110));
        var page = new Rectangle(160, 96, 640, 400);
        using (var shadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0))) g.FillRectangle(shadow, page with { X = page.X + 4, Y = page.Y + 6 });
        g.FillRectangle(Brushes.White, page);
        using (var bar = new SolidBrush(Color.FromArgb(241, 243, 246))) g.FillRectangle(bar, page with { Height = 36 });
        using var titleFont = new Font("Segoe UI", 10f);
        using var headFont = new Font("Segoe UI Semibold", 15f);
        using var bodyFont = new Font("Segoe UI", 11f);
        using (var ink = new SolidBrush(Color.FromArgb(60, 64, 70))) g.DrawString(turkish ? "notlar.txt - Not Defteri" : "notes.txt - Notepad", titleFont, ink, new Point(page.X + 14, page.Y + 9));
        using (var ink = new SolidBrush(Color.FromArgb(25, 28, 33))) g.DrawString(turkish ? "Toplantı notları" : "Meeting notes", headFont, ink, new Point(page.X + 36, page.Y + 64));
        string[] lines = turkish
            ? new[] { "Fatura numarası: TR-2026-0418", "Teslim tarihi: 14 Kasım, sabah 10:30", "Adres: Atatürk Cad. No 12, İzmir" }
            : new[] { "Invoice number: INV-2026-0418", "Delivery date: 14 November, 10:30 am", "Address: 12 Harbour Street, Portsmouth" };
        int y = page.Y + 148;
        foreach (string line in lines)
        {
            using (var ink = new SolidBrush(Color.FromArgb(40, 44, 52))) g.DrawString(line, bodyFont, ink, new Point(page.X + 44, y));
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
