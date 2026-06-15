namespace WinTextCapture;

internal static class CaptureMessageFormatter
{
    public static string Format(CaptureOutcome outcome) =>
        outcome.Status switch
        {
            CaptureStatus.Copied => FormatCopied(outcome.Text),
            CaptureStatus.NoText => Strings.NoText,
            CaptureStatus.ClipboardUnchanged => Strings.ClipboardUnchanged,
            CaptureStatus.Error => Strings.CaptureError,
            CaptureStatus.Busy => Strings.CaptureBusy,
            _ => string.Empty,
        };

    private static string FormatCopied(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Strings.Copied;

        string preview = string.Join(" ", text.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries));
        if (preview.Length > 80)
            preview = preview[..77] + "...";

        return string.Format(Strings.CopiedPreview, preview);
    }
}
