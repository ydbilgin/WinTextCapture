using System.Text;

namespace WinTextCapture;

/// <summary>Best-effort, bounded persistence for last-chance exception diagnostics.</summary>
internal static class CrashLogger
{
    private const int MaxLogBytes = 256 * 1024;
    private static readonly object Sync = new();

    public static void Write(Exception exception, bool fatal)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(Settings.Dir);
                string path = Path.Combine(Settings.Dir, "error.log");
                File.AppendAllText(path, CrashLog.Format(DateTime.UtcNow, App.Version, fatal, exception));
                TrimToMostRecentBytes(path);
            }
        }
        catch
        {
            // Last-chance diagnostics must never replace the original application failure.
        }
    }

    private static void TrimToMostRecentBytes(string path)
    {
        var file = new FileInfo(path);
        if (file.Length <= MaxLogBytes)
            return;

        byte[] contents = File.ReadAllBytes(path);
        int start = contents.Length - MaxLogBytes;
        while (start < contents.Length && (contents[start] & 0xC0) == 0x80)
            start++;

        File.WriteAllBytes(path, contents[start..]);
    }
}
