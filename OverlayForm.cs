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

    public CaptureOutcome Result { get; private set; } = CaptureOutcome.Cancelled;

    public OverlayForm(Settings settings)
    {
        _settings = settings;

        FormBorderStyle = FormBorderStyle.None;
        Bounds = VirtualScreenBounds();
        TopMost = true;
        Cursor = Cursors.Cross;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        KeyPreview = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;

        _screenshot = new Bitmap(Bounds.Width, Bounds.Height);
        using (var g = Graphics.FromImage(_screenshot))
            g.CopyFromScreen(Bounds.Location, Point.Empty, Bounds.Size);

        _dimmedScreenshot = CreateDimmedScreenshot(_screenshot);
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUpAsync;
        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
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

    private static Rectangle VirtualScreenBounds()
    {
        int x = Native.GetSystemMetrics(Native.SM_XVIRTUALSCREEN);
        int y = Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN);
        int w = Native.GetSystemMetrics(Native.SM_CXVIRTUALSCREEN);
        int h = Native.GetSystemMetrics(Native.SM_CYVIRTUALSCREEN);
        return new Rectangle(x, y, w, h);
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        _startPoint = e.Location;
        _selection = Rectangle.Empty;
        _dragging = true;
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;

        var previous = _selection;
        var next = new Rectangle(
            Math.Min(_startPoint.X, e.X),
            Math.Min(_startPoint.Y, e.Y),
            Math.Abs(_startPoint.X - e.X),
            Math.Abs(_startPoint.Y - e.Y));
        if (next == previous) return;

        _selection = next;
        Invalidate(DamageRect(previous, _selection));
    }

    private async void OnMouseUpAsync(object? sender, MouseEventArgs e)
    {
        _dragging = false;
        Hide();

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

            Result = await ExtractTextAsync(cropped);
        }
        catch
        {
            Result = CaptureOutcome.Error;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var clip = e.ClipRectangle;
        e.Graphics.DrawImage(_dimmedScreenshot, clip, clip, GraphicsUnit.Pixel);

        if (_selection.Width <= 0 || _selection.Height <= 0) return;

        e.Graphics.DrawImage(_screenshot, _selection, _selection, GraphicsUnit.Pixel);
        using var pen = new Pen(Color.FromArgb(80, 220, 255), 2f);
        e.Graphics.DrawRectangle(pen, _selection);
    }

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
            using var ocrBitmap = OcrImagePreprocessor.Prepare(bitmap);
            ocrBitmap.Save(tempFile, ImageFormat.Png);

            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(tempFile);
            using var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
            var engine = CreateOcrEngine();
            if (engine == null) return CaptureOutcome.Error;

            var result = await engine.RecognizeAsync(softwareBitmap);
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
            _screenshot.Dispose();
            _dimmedScreenshot.Dispose();
        }
        base.Dispose(disposing);
    }
}
