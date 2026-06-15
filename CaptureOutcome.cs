namespace WinTextCapture;

internal enum CaptureStatus
{
    Cancelled,
    Copied,
    NoText,
    ClipboardUnchanged,
    Error,
    Busy
}

internal sealed record CaptureOutcome(CaptureStatus Status, string? Text = null)
{
    public static readonly CaptureOutcome Cancelled = new(CaptureStatus.Cancelled);
    public static readonly CaptureOutcome NoText = new(CaptureStatus.NoText);
    public static readonly CaptureOutcome ClipboardUnchanged = new(CaptureStatus.ClipboardUnchanged);
    public static readonly CaptureOutcome Error = new(CaptureStatus.Error);
    public static readonly CaptureOutcome Busy = new(CaptureStatus.Busy);

    public static CaptureOutcome Copied(string text) => new(CaptureStatus.Copied, text);
}
