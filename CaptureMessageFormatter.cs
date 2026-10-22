using System.Globalization;

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

    /// <summary>Bold first line, optional detail line and accent kind for the toast; null when nothing is shown.</summary>
    public static ToastContent? Toast(CaptureOutcome outcome) =>
        outcome.Status switch
        {
            CaptureStatus.Copied => new ToastContent(Strings.ToastCopied,
                string.IsNullOrWhiteSpace(outcome.Text) ? Strings.Copied : Preview(outcome.Text), Success: true),
            CaptureStatus.NoText => new ToastContent(Strings.ToastNoText, Strings.ToastNoTextBody, Success: false),
            CaptureStatus.ClipboardUnchanged => new ToastContent(Strings.ToastClipboardUnchanged, Strings.ClipboardUnchanged, Success: false),
            CaptureStatus.Error => new ToastContent(Strings.ToastError, "", Success: false),
            CaptureStatus.Busy => new ToastContent(Strings.ToastBusy, "", Success: false),
            _ => null,
        };

    private static string FormatCopied(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Strings.Copied;

        return string.Format(Strings.CopiedPreview, Preview(text));
    }

    /// <summary>Whitespace collapsed to single spaces, cut at 80 characters on a text-element boundary.</summary>
    private static string Preview(string text)
    {
        string preview = string.Join(" ", text.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries));
        if (preview.Length > 80)
        {
            int length = 0;
            var elements = StringInfo.GetTextElementEnumerator(preview);
            while (elements.MoveNext())
            {
                int end = elements.ElementIndex + elements.GetTextElement().Length;
                if (end > 77) break;
                length = end;
            }
            preview = preview[..length] + "...";
        }

        return preview;
    }
}
