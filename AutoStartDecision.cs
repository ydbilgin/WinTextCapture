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
    /// <summary>
    /// Startup reconciliation. The presence of the registry value — not a persisted preference — is what
    /// decides whether autostart is on, because that value is the thing Windows actually obeys. The only
    /// repair made without the user asking is a BROKEN entry: one whose target no longer exists, which is
    /// exactly what a moved, cleaned, or reinstalled build leaves behind. An entry pointing at a different
    /// copy that DOES exist is left alone — the user may have chosen that copy deliberately, and silently
    /// hijacking it would be a surprise rather than a repair.
    /// </summary>
    public static AutoStartAction EvaluateStartup(
        string? currentRegistryCommand,
        string currentExecutablePath,
        bool storedTargetExists)
    {
        if (string.IsNullOrWhiteSpace(currentRegistryCommand))
            return AutoStartAction.LeaveAsIs;   // autostart is off; turning it on stays the user's decision

        if (PointsTo(currentRegistryCommand, currentExecutablePath))
            return AutoStartAction.LeaveAsIs;

        return storedTargetExists ? AutoStartAction.LeaveAsIs : AutoStartAction.Write;
    }

    /// <summary>The user changed the setting, so their choice is authoritative and points at this copy.</summary>
    public static AutoStartAction EvaluateUserChange(
        bool enabled,
        string? currentRegistryCommand,
        string currentExecutablePath)
    {
        if (!enabled)
            return currentRegistryCommand is null ? AutoStartAction.LeaveAsIs : AutoStartAction.Remove;

        return PointsTo(currentRegistryCommand, currentExecutablePath)
            ? AutoStartAction.LeaveAsIs
            : AutoStartAction.Write;
    }

    /// <summary>The executable a Run value points at, or null when it cannot be read as a path.</summary>
    public static string? TargetPath(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return null;

        string storedPath = command.Trim();
        if (storedPath.StartsWith('"'))
        {
            int closingQuote = storedPath.IndexOf('"', 1);
            if (closingQuote < 0)
                return null;

            storedPath = storedPath[1..closingQuote];
        }

        try
        {
            return Path.GetFullPath(storedPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static bool PointsTo(string? command, string executablePath)
    {
        string? storedPath = TargetPath(command);
        if (storedPath is null)
            return false;

        try
        {
            return string.Equals(storedPath, Path.GetFullPath(executablePath), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
