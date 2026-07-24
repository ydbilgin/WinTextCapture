using Microsoft.Win32;

namespace WinTextCapture;

/// <summary>Adds/removes the app from the per-user Windows startup (HKCU ...\Run).</summary>
internal static class AutoStart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = App.Name;

    public static bool IsEnabled() => ReadCommand() is not null;

    public static void ReconcileStartup(bool desiredEnabled)
    {
        string? currentCommand = ReadCommand();
        Apply(AutoStartDecision.EvaluateStartup(
            desiredEnabled,
            currentCommand,
            Application.ExecutablePath));
    }

    public static void ApplyUserChange(bool enabled)
    {
        string? currentCommand = ReadCommand();
        Apply(AutoStartDecision.EvaluateUserChange(
            enabled,
            currentCommand,
            Application.ExecutablePath));
    }

    private static string? ReadCommand()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) as string;
    }

    private static void Apply(AutoStartAction action)
    {
        if (action == AutoStartAction.LeaveAsIs)
            return;

        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true)
            ?? throw new InvalidOperationException("The Windows startup registry key could not be opened.");

        if (action == AutoStartAction.Write)
            key.SetValue(ValueName, $"\"{Application.ExecutablePath}\"");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
