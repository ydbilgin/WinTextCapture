namespace WinTextCapture;

internal enum AutoStartAction
{
    LeaveAsIs,
    Write,
    Remove,
}

/// <summary>Decides registry changes without accessing Windows or the filesystem.</summary>
internal static class AutoStartDecision
{
    public static AutoStartAction EvaluateStartup(
        bool desiredEnabled,
        string? currentRegistryCommand,
        string currentExecutablePath)
    {
        if (!desiredEnabled)
            return AutoStartAction.LeaveAsIs;

        return PointsTo(currentRegistryCommand, currentExecutablePath)
            ? AutoStartAction.LeaveAsIs
            : AutoStartAction.Write;
    }

    public static AutoStartAction EvaluateUserChange(
        bool enabled,
        string? currentRegistryCommand,
        string currentExecutablePath)
    {
        if (!enabled)
            return currentRegistryCommand is null ? AutoStartAction.LeaveAsIs : AutoStartAction.Remove;

        return EvaluateStartup(desiredEnabled: true, currentRegistryCommand, currentExecutablePath);
    }

    private static bool PointsTo(string? command, string executablePath)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        string storedPath = command.Trim();
        if (storedPath.StartsWith('"'))
        {
            int closingQuote = storedPath.IndexOf('"', 1);
            if (closingQuote < 0)
                return false;

            storedPath = storedPath[1..closingQuote];
        }

        try
        {
            return string.Equals(
                Path.GetFullPath(storedPath),
                Path.GetFullPath(executablePath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
