using System.Threading;

namespace WinTextCapture;

internal static class Program
{
    private const string SingleInstanceMutexName = "Global\\WinTextCapture_SingleInstance_3F1A";

    [STAThread]
    private static void Main(string[] args)
    {
        Native.SetProcessDpiAwarenessContext(Native.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Dev/CI helper: render the settings window off-screen to a PNG (for docs). Runs before the
        // single-instance check so it works while the tray app is already running.
        if (args.Length >= 2 && args[0] == "--screenshot")
        {
            string? theme = args.Length >= 3 ? args[2] : null;
            string lang = args.Length >= 4 ? args[3] : "en";
            Screenshot.CaptureSettings(args[1], theme, lang);
            return;
        }

        // Allow only one running instance. A second launch (double-click,
        // start menu, autostart) exits immediately instead of stacking tray icons.
        using var singleInstance = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool isFirstInstance);
        if (!isFirstInstance)
            return;

        try
        {
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
