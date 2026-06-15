using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace WinTextCapture;

internal static class AppIcon
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon Create()
    {
        using var bitmap = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);

        using var shell = RoundedRect(6, 6, 52, 52, 14);
        using var shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
        using var back = new LinearGradientBrush(
            new Rectangle(6, 6, 52, 52),
            Color.FromArgb(24, 45, 74),
            Color.FromArgb(15, 103, 152),
            LinearGradientMode.ForwardDiagonal);
        using var shadowPath = RoundedRect(8, 9, 50, 50, 14);
        g.FillPath(shadow, shadowPath);
        g.FillPath(back, shell);

        using var cropPen = new Pen(Color.FromArgb(244, 248, 250), 5f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        DrawCorner(g, cropPen, 17, 18, 12, 12, 1, 1);
        DrawCorner(g, cropPen, 47, 18, 12, 12, -1, 1);
        DrawCorner(g, cropPen, 17, 47, 12, 12, 1, -1);
        DrawCorner(g, cropPen, 47, 47, 12, 12, -1, -1);

        using var linePen = new Pen(Color.FromArgb(216, 237, 247), 4f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawLine(linePen, 23, 28, 42, 28);
        g.DrawLine(linePen, 23, 36, 38, 36);

        using var dot = new SolidBrush(Color.FromArgb(83, 211, 255));
        g.FillEllipse(dot, 41, 39, 8, 8);

        IntPtr handle = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static void DrawCorner(Graphics g, Pen pen, int x, int y, int w, int h, int sx, int sy)
    {
        g.DrawLine(pen, x, y, x + sx * w, y);
        g.DrawLine(pen, x, y, x, y + sy * h);
    }

    private static GraphicsPath RoundedRect(int x, int y, int width, int height, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + width - d, y, d, d, 270, 90);
        path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
        path.AddArc(x, y + height - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
