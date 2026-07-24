namespace WinTextCapture;

/// <summary>Formats crash information without performing file or UI operations.</summary>
internal static class CrashLog
{
    public static string Format(DateTime utc, string version, bool fatal, Exception exception)
    {
        string disposition = fatal ? "FATAL" : "HANDLED";
        return $"[{utc.ToUniversalTime():O}] WinTextCapture {version} | {disposition}{Environment.NewLine}" +
               $"{exception}{Environment.NewLine}---{Environment.NewLine}";
    }
}
