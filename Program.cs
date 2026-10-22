using System.Threading;

namespace WinTextCapture;

internal static class Program
{
    private const string SingleInstanceMutexName = "Local\\WinTextCapture_SingleInstance_3F1A";

    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += OnThreadException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        try
        {
            return Run(args);
        }
        catch (Exception ex)
        {
            CrashLogger.Write(ex, fatal: true);
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        Native.SetProcessDpiAwarenessContext(Native.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Dev/CI helper: render the settings window off-screen to a PNG (for docs). Runs before the
        // single-instance check so it works while the tray app is already running.
        // Arguments: <mode> <png> [Light|Dark] [en|tr] [variant]. Every mode uses default settings
        // and synthetic content, and keeps its window off the visible desktop.
        if (args.Length >= 2 && args[0].StartsWith("--screenshot", StringComparison.Ordinal))
        {
            string? theme = args.Length >= 3 ? args[2] : null;
            string lang = args.Length >= 4 ? args[3] : "en";
            string? variant = args.Length >= 5 ? args[4] : null;
            switch (args[0])
            {
                case "--screenshot": Screenshot.CaptureSettings(args[1], theme, lang); break;
                case "--screenshot-tray": Screenshot.CaptureTrayMenu(args[1], theme, lang); break;
                case "--screenshot-overlay": Screenshot.CaptureOverlay(args[1], theme, lang); break;
                default: return 2;
            }
            return 0;
        }

        // Allow only one running instance. A second launch (double-click,
        // start menu, autostart) exits immediately instead of stacking tray icons.
        using var singleInstance = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool isFirstInstance);
        if (!isFirstInstance)
            return 0;

        Application.Run(new TrayApplicationContext());
        return 0;
    }

    private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
    {
        CrashLogger.Write(e.Exception, fatal: false);

        // WinForms UI exceptions, including capture-path failures, are logged and handled so the
        // tray remains available for another capture instead of disappearing without explanation.
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        Exception exception = e.ExceptionObject as Exception
            ?? new InvalidOperationException($"Unhandled non-exception object: {e.ExceptionObject}");
        CrashLogger.Write(exception, fatal: true);

        // This boundary represents a corrupt or unrecoverable process state. The runtime will
        // terminate after the event; set a failing exit status and request orderly WinForms exit.
        Environment.ExitCode = 1;
        Application.Exit();
    }
}
