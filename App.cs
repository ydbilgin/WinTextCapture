namespace WinTextCapture;

/// <summary>App-wide constants.</summary>
internal static class App
{
    public const string Name = "WinTextCapture";
    public static string Version =>
        typeof(App).Assembly.GetName().Version?.ToString(3) ?? "unknown";
}
