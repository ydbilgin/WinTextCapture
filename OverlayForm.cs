using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace WinTextCapture;

internal sealed class OverlayForm : Form
{
    private readonly Settings _settings;
    private readonly Bitmap _screenshot;
    private readonly Bitmap _dimmedScreenshot;
    private Point _startPoint;
    private Rectangle _selection;
    private bool _dragging;
    private bool _processing;
    private bool _cancelled;
    private readonly Func<Bitmap, Task<CaptureOutcome>> _extract;
    private readonly Theme _theme;
    private readonly Font _hintFont;
    private readonly Font _badgeFont;

    /// <summary>Client-area rectangle of the monitor under the cursor; the hint pill sits at its top.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal Rectangle HintArea { get; set; }

    public CaptureOutcome Result { get; private set; } = CaptureOutcome.Cancelled;

    public OverlayForm(Settings settings) : this(settings, null, null) { }

    internal OverlayForm(Settings settings, Bitmap? screenshot, Func<Bitmap, Task<CaptureOutcome>>? extract)
    {
        _settings = settings;
        _extract = extract ?? ExtractTextAsync;
        _theme = Theming.Resolve(settings.Theme);

        FormBorderStyle = FormBorderStyle.None;
        AutoScaleMode = AutoScaleMode.None; // screenshot and mouse coordinates are physical pixels
        Bounds = VirtualScreenBounds();
        TopMost = true;
        Cursor = Cursors.Cross;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        KeyPreview = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;

        if (screenshot is null)
        {
            _screenshot = CaptureScreenshot(Bounds);
        }
        else
        {
            _screenshot = (Bitmap)screenshot.Clone();
            Size = screenshot.Size;
        }

        try { _dimmedScreenshot = CreateDimmedScreenshot(_screenshot); }
        catch { _screenshot.Dispose(); throw; }

        // Overlay coordinates are physical pixels, so text sizes follow the window's DPI by hand.
        float scale = DeviceDpi / 96f;
        _hintFont = new Font("Segoe UI", 14f * scale, GraphicsUnit.Pixel);
        _badgeFont = new Font("Segoe UI Semibold", 12f * scale, GraphicsUnit.Pixel);
        HintArea = MonitorUnderCursor();
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUpAsync;
        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                _cancelled = true;
                Result = CaptureOutcome.Cancelled;
                Close();
            }
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Native.SetWindowPos(Handle, Native.HWND_TOPMOST, Bounds.Left, Bounds.Top, Bounds.Width, Bounds.Height, Native.SWP_SHOWWINDOW);
        Native.SetForegroundWindow(Handle);
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        // A screen-spanning screenshot cannot follow one monitor's suggested logical-size rectangle.
        e.Cancel = true;
        base.OnDpiChanged(e);
    }

    private static Rectangle VirtualScreenBounds()
    {
        int x = Native.GetSystemMetrics(Native.SM_XVIRTUALSCREEN);
        int y = Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN);
        int w = Native.GetSystemMetrics(Native.SM_CXVIRTUALSCREEN);
        int h = Native.GetSystemMetrics(Native.SM_CYVIRTUALSCREEN);
        return new Rectangle(x, y, w, h);
    }

    private Rectangle MonitorUnderCursor()
    {
        var client = new Rectangle(Point.Empty, _screenshot.Size);
        var monitor = Screen.FromPoint(Cursor.Position).Bounds;
        monitor.Offset(-Bounds.Left, -Bounds.Top);
        var area = Rectangle.Intersect(client, monitor);
        return area.Width > 0 && area.Height > 0 ? area : client;
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _processing) return;
        _startPoint = e.Location;
        _selection = Rectangle.Empty;
        _dragging = true;
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;

        var previous = _selection;
        var next = Rectangle.Intersect(new Rectangle(Point.Empty, _screenshot.Size), new Rectangle(
            Math.Min(_startPoint.X, e.X),
            Math.Min(_startPoint.Y, e.Y),
            Math.Abs(_startPoint.X - e.X),
            Math.Abs(_startPoint.Y - e.Y)));
        if (next == previous) return;

        _selection = next;
        Invalidate(DamageRect(previous, _selection));
        if (!previous.IsEmpty) Invalidate(Inflated(BadgeBounds(previous), 2));
        if (!_selection.IsEmpty) Invalidate(Inflated(BadgeBounds(_selection), 2));
    }

    private async void OnMouseUpAsync(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || !_dragging || _processing) return;
        OnMouseMove(sender, e); // the release can be newer than the last coalesced WM_MOUSEMOVE
        _dragging = false;
        _processing = true;
        // Hiding a modal form ends ShowDialog immediately and disposes this form while OCR is pending.
        // A transparent visible form keeps ownership until the asynchronous operation completes.
        Opacity = 0;

        if (_selection.Width <= 10 || _selection.Height <= 10)
        {
            Result = CaptureOutcome.Cancelled;
            Close();
            return;
        }

        try
        {
            using var cropped = new Bitmap(_selection.Width, _selection.Height);
            using (var g = Graphics.FromImage(cropped))
                g.DrawImage(_screenshot, new Rectangle(0, 0, cropped.Width, cropped.Height), _selection, GraphicsUnit.Pixel);

            var outcome = await _extract(cropped);
            if (_cancelled || IsDisposed) return;
            Result = outcome;
        }
        catch (Exception ex)
        {
            CrashLogger.Write(ex, fatal: false);
            if (_cancelled || IsDisposed) return;
            Result = CaptureOutcome.Error;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        PaintOverlay(e.Graphics, e.ClipRectangle);
    }

    /// <summary>
    /// Draws the overlay surface only: the dimmed screenshot, the clear selection with its accent outline
    /// and size badge, and the hint pill. The screenshot bitmaps are read, never drawn on.
    /// </summary>
    internal void PaintOverlay(Graphics g, Rectangle clip)
    {
        g.DrawImage(_dimmedScreenshot, clip, clip, GraphicsUnit.Pixel);

        if (_selection.Width > 0 && _selection.Height > 0)
        {
            g.DrawImage(_screenshot, _selection, _selection, GraphicsUnit.Pixel);
            using var pen = new Pen(_theme.Accent, 2f);
            g.DrawRectangle(pen, _selection);
            DrawBadge(g, _selection);
        }

        DrawHint(g);
    }

    /// <summary>Test and documentation hook: the selection a drag would have made.</summary>
    internal void PreviewSelection(Rectangle selection) =>
        _selection = Rectangle.Intersect(new Rectangle(Point.Empty, _screenshot.Size), selection);

    internal static string SizeText(Rectangle selection) => $"{selection.Width} × {selection.Height}";

    internal Rectangle HintBounds()
    {
        var text = TextRenderer.MeasureText(Strings.OverlayHint, _hintFont, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        int padX = (int)Math.Round(16 * DeviceDpi / 96f), padY = (int)Math.Round(8 * DeviceDpi / 96f);
        int top = (int)Math.Round(24 * DeviceDpi / 96f);
        var size = new Size(text.Width + padX * 2, text.Height + padY * 2);
        return new Rectangle(HintArea.Left + (HintArea.Width - size.Width) / 2, HintArea.Top + top, size.Width, size.Height);
    }

    internal Rectangle BadgeBounds(Rectangle selection)
    {
        if (selection.Width <= 0 || selection.Height <= 0) return Rectangle.Empty;
        var text = TextRenderer.MeasureText(SizeText(selection), _badgeFont, Size.Empty, TextFormatFlags.NoPadding);
        int pad = (int)Math.Round(6 * DeviceDpi / 96f), gap = (int)Math.Round(6 * DeviceDpi / 96f);
        var size = new Size(text.Width + pad * 2, text.Height + pad);
        var client = new Rectangle(Point.Empty, _screenshot.Size);
        int y = selection.Bottom + gap;
        if (y + size.Height > client.Bottom) y = selection.Top - gap - size.Height;   // no room below: above
        if (y < client.Top) y = selection.Bottom - gap - size.Height;                 // no room above: inside
        int x = Math.Clamp(selection.Left, client.Left, Math.Max(client.Left, client.Right - size.Width));
        return new Rectangle(x, y, size.Width, size.Height);
    }

    private void DrawBadge(Graphics g, Rectangle selection)
    {
        var bounds = BadgeBounds(selection);
        var smoothing = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = Theming.Rounded(bounds, Math.Max(3, bounds.Height / 4)))
        using (var fill = new SolidBrush(_theme.Accent))
            g.FillPath(fill, path);
        g.SmoothingMode = smoothing;
        TextRenderer.DrawText(g, SizeText(selection), _badgeFont, bounds, _theme.AccentText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    private void DrawHint(Graphics g)
    {
        var bounds = HintBounds();
        var smoothing = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = Theming.Rounded(bounds, bounds.Height / 2))
        {
            using (var fill = new SolidBrush(Color.FromArgb(240, _theme.Surface))) g.FillPath(fill, path);
            using (var pen = new Pen(_theme.BorderStrong)) g.DrawPath(pen, path);
        }
        g.SmoothingMode = smoothing;
        TextRenderer.DrawText(g, Strings.OverlayHint, _hintFont, bounds, _theme.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
    }

    private static Rectangle Inflated(Rectangle r, int by) { r.Inflate(by, by); return r; }

    private static Bitmap CreateDimmedScreenshot(Bitmap source)
    {
        var dimmed = new Bitmap(source.Width, source.Height);
        using var g = Graphics.FromImage(dimmed);
        g.DrawImageUnscaled(source, Point.Empty);
        using var dim = new SolidBrush(Color.FromArgb(125, 0, 0, 0));
        g.FillRectangle(dim, new Rectangle(Point.Empty, source.Size));
        return dimmed;
    }

    private static Rectangle DamageRect(Rectangle a, Rectangle b)
    {
        var damage = a.IsEmpty ? b : Rectangle.Union(a, b);
        damage.Inflate(8, 8);
        return damage;
    }

    private async Task<CaptureOutcome> ExtractTextAsync(Bitmap bitmap)
    {
        string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        try
        {
            using var ocrBitmap = OcrImagePreprocessor.Prepare(bitmap, checked((int)OcrEngine.MaxImageDimension));
            ocrBitmap.Save(tempFile, ImageFormat.Png);

            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(tempFile);
            using var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
            var engine = CreateOcrEngine();
            if (engine == null) return CaptureOutcome.Error;

            var result = await engine.RecognizeAsync(softwareBitmap);
            if (_cancelled || IsDisposed) return CaptureOutcome.Cancelled;
            if (string.IsNullOrWhiteSpace(result.Text))
                return CaptureOutcome.NoText;

            string text = result.Text.Trim();
            return ClipboardWriter.SetTextWithRetry(text)
                ? CaptureOutcome.Copied(text)
                : CaptureOutcome.ClipboardUnchanged;
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            catch
            {
                // Temporary file cleanup is best-effort.
            }
        }
    }

    private OcrEngine? CreateOcrEngine()
    {
        if (_settings.OcrLanguage == "auto")
            return OcrEngine.TryCreateFromUserProfileLanguages()
                ?? OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("tr-TR"));

        return OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language(_settings.OcrLanguage))
            ?? OcrEngine.TryCreateFromUserProfileLanguages();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancelled = true;
            _screenshot.Dispose();
            _dimmedScreenshot.Dispose();
            _hintFont.Dispose();
            _badgeFont.Dispose();
        }
        base.Dispose(disposing);
    }

    private static Bitmap CaptureScreenshot(Rectangle bounds)
    {
        var bitmap = new Bitmap(bounds.Width, bounds.Height);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            return bitmap;
        }
        catch { bitmap.Dispose(); throw; }
    }
}
