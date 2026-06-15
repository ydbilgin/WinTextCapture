using System.Drawing.Drawing2D;

namespace WinTextCapture;

internal static class OcrImagePreprocessor
{
    private const int MaxDimension = 3200;

    public static Bitmap Prepare(Bitmap source)
    {
        int scale = ScaleFor(source.Width, source.Height);
        if (scale <= 1)
            return (Bitmap)source.Clone();

        var scaled = new Bitmap(source.Width * scale, source.Height * scale);
        using var g = Graphics.FromImage(scaled);
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.SmoothingMode = SmoothingMode.None;
        g.DrawImage(source, new Rectangle(0, 0, scaled.Width, scaled.Height));
        return scaled;
    }

    public static int ScaleFor(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return 1;

        int scale = height < 90 ? 3 : 2;
        while (scale > 1 && (width * scale > MaxDimension || height * scale > MaxDimension))
            scale--;

        return scale;
    }
}
