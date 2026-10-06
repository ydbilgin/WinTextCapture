using System.Drawing.Drawing2D;

namespace WinTextCapture;

internal static class OcrImagePreprocessor
{
    private const int MaxDimension = 3200;

    public static Bitmap Prepare(Bitmap source, int maxDimension = MaxDimension)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDimension);
        int scale = ScaleFor(source.Width, source.Height, Math.Min(maxDimension, MaxDimension));
        var size = new Size(source.Width * scale, source.Height * scale);
        int largestDimension = Math.Max(source.Width, source.Height);
        if (largestDimension > maxDimension)
        {
            size = new Size(
                Math.Max(1, (int)((long)source.Width * maxDimension / largestDimension)),
                Math.Max(1, (int)((long)source.Height * maxDimension / largestDimension)));
        }

        if (size == source.Size)
            return (Bitmap)source.Clone();

        var scaled = new Bitmap(size.Width, size.Height);
        using var g = Graphics.FromImage(scaled);
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.SmoothingMode = SmoothingMode.None;
        g.DrawImage(source, new Rectangle(0, 0, scaled.Width, scaled.Height));
        return scaled;
    }

    public static int ScaleFor(int width, int height, int maxDimension = MaxDimension)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDimension);
        if (width <= 0 || height <= 0)
            return 1;

        int scale = height < 90 ? 3 : 2;
        while (scale > 1 && (width > maxDimension / scale || height > maxDimension / scale))
            scale--;

        return scale;
    }
}
