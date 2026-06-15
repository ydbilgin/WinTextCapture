namespace WinTextCapture;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            Native.SetProcessDpiAwarenessContext(Native.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApplicationContext());
        }
        catch (Exception ex)
        {
            try
            {
                Directory.CreateDirectory(Settings.Dir);
                File.WriteAllText(Path.Combine(Settings.Dir, "error.log"), ex.ToString());
            }
            catch
            {
                // Last-chance logging must never crash startup.
            }
        }
    }
}
