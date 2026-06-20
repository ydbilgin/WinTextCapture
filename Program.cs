using System.Threading;

namespace WinTextCapture;

internal static class Program
{
    private const string SingleInstanceMutexName = "Global\\WinTextCapture_SingleInstance_3F1A";

    [STAThread]
    private static void Main()
    {
        // Allow only one running instance. A second launch (double-click,
        // start menu, autostart) exits immediately instead of stacking tray icons.
        using var singleInstance = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool isFirstInstance);
        if (!isFirstInstance)
            return;

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
