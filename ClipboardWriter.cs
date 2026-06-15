using System.Runtime.InteropServices;

namespace WinTextCapture;

internal static class ClipboardWriter
{
    public static bool SetTextWithRetry(string text) =>
        SetTextWithRetry(text, value => Clipboard.SetDataObject(value, true), Clipboard.GetText, Thread.Sleep);

    public static bool SetTextWithRetry(string text, Action<string> setText, Func<string> getText, Action<int> delay, int attempts = 6)
    {
        if (attempts <= 0)
            throw new ArgumentOutOfRangeException(nameof(attempts));

        for (int i = 0; i < attempts; i++)
        {
            try
            {
                setText(text);
                if (SameText(text, getText()))
                    return true;
            }
            catch (ExternalException) when (i + 1 < attempts)
            {
                delay(35);
                continue;
            }

            if (i + 1 < attempts)
                delay(35);
        }

        return false;
    }

    private static bool SameText(string expected, string actual) =>
        string.Equals(Normalize(expected), Normalize(actual), StringComparison.Ordinal);

    private static string Normalize(string value) =>
        value.Replace("\r\n", "\n").Replace("\r", "\n");
}
