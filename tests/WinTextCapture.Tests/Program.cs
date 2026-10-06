using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinTextCapture;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    [STAThread]
    private static int Main()
    {
        Run("default hotkey triggers once, consumes repeats and key-up, then resets", HotkeyTriggersOnceAndResets);
        Run("wrong modifiers pass through", WrongModifiersPass);
        Run("invalid binding passes through", InvalidBindingPasses);
        Run("key-up resets even if settings changed while held", SettingsChangeWhileHeldResets);
        Run("held hotkey repeats stay consumed after modifiers are released", HotkeyRepeatsAfterModifierRelease);
        Run("held hotkey repeats stay consumed after settings change", HotkeyRepeatsAfterSettingsChange);
        Run("a different key passes while the original hotkey remains held", DifferentKeyWhileHotkeyHeldPasses);
        Run("capture messages include useful copied preview", CopiedMessageIncludesPreview);
        Run("copied previews preserve Unicode text elements at the length limit", CopiedPreviewPreservesTextElements);
        Run("capture messages expose no-text, error and busy states", NonCopiedMessagesAreDeterministic);
        Run("clipboard retry succeeds after transient lock", ClipboardRetrySucceedsAfterTransientLock);
        Run("clipboard retry rethrows after final failure", ClipboardRetryRethrowsAfterFinalFailure);
        Run("clipboard retry detects unchanged clipboard", ClipboardRetryDetectsUnchangedClipboard);
        Run("OCR preprocessing upscales small selections within bounds", OcrPreprocessorScalesSmallSelections);
        Run("OCR preprocessing bounds oversized landscape selections", OcrPreprocessorBoundsLandscape);
        Run("OCR preprocessing bounds oversized portrait selections", OcrPreprocessorBoundsPortrait);
        Run("OCR scale calculation does not overflow for large dimensions", OcrScaleDoesNotOverflow);
        Run("OCR preprocessing respects a supplied engine limit", OcrPreprocessorRespectsEngineLimit);
        Run("autostart leaves an absent entry alone at startup", AutoStartLeavesAbsentEntryAlone);
        Run("autostart repairs an entry whose executable is gone", AutoStartRepairsMissingExecutable);
        Run("autostart keeps the current executable entry", AutoStartKeepsCurrentExecutable);
        Run("autostart does not hijack another existing install", AutoStartDoesNotHijackAnotherInstall);
        Run("autostart treats quoted and unquoted current paths equally", AutoStartHandlesQuotedAndUnquotedPaths);
        Run("autostart writes this copy when the user enables it", AutoStartWritesWhenUserEnables);
        Run("autostart removes an existing entry after user disable", AutoStartRemovesEntryAfterUserDisable);
        Run("crash log formats handled exceptions with diagnostics", CrashLogFormatsHandledException);
        Run("crash log distinguishes fatal exceptions", CrashLogFormatsFatalException);
        Run("application version comes from stamped assembly metadata", ApplicationVersionIsStamped);
        Run("settings replace explicit null preferences with defaults", SettingsNullPreferencesUseDefaults);
        Run("settings reject malformed OCR language tags", SettingsMalformedOcrTagUsesDefault);
        Run("settings recover an invalid shortcut without losing other preferences", SettingsInvalidShortcutUsesDefault);
        Run("modifier-only and undefined trigger keys are invalid", ModifierAndUndefinedKeysAreInvalid);
        Run("settings preserve valid preferences and recover broken JSON", SettingsValidAndBrokenJson);
        Run("settings action buttons do not overlap appearance controls", SettingsButtonsDoNotOverlapAppearance);

        Console.WriteLine($"Passed: {_passed}");
        Console.WriteLine($"Failed: {_failed}");
        return _passed > 0 && _failed == 0 ? 0 : 1;
    }

    private static void HotkeyTriggersOnceAndResets()
    {
        var state = new KeyboardHookState();
        var binding = new HotkeyBinding(Keys.T, Ctrl: false, Alt: false, Shift: true, Win: true);

        Equal(HookAction.Trigger, state.Evaluate(binding, Keys.T, keyDown: true, keyUp: false, ctrl: false, alt: false, shift: true, win: true));
        Equal(HookAction.Consume, state.Evaluate(binding, Keys.T, keyDown: true, keyUp: false, ctrl: false, alt: false, shift: true, win: true));
        Equal(HookAction.Consume, state.Evaluate(binding, Keys.T, keyDown: false, keyUp: true, ctrl: false, alt: false, shift: false, win: false));
        Equal(HookAction.Trigger, state.Evaluate(binding, Keys.T, keyDown: true, keyUp: false, ctrl: false, alt: false, shift: true, win: true));
    }

    private static void WrongModifiersPass()
    {
        var state = new KeyboardHookState();
        var binding = new HotkeyBinding(Keys.T, Ctrl: false, Alt: false, Shift: true, Win: true);
        Equal(HookAction.Pass, state.Evaluate(binding, Keys.T, keyDown: true, keyUp: false, ctrl: true, alt: false, shift: true, win: true));
    }

    private static void InvalidBindingPasses()
    {
        var state = new KeyboardHookState();
        var binding = new HotkeyBinding(Keys.None, Ctrl: false, Alt: false, Shift: false, Win: false);
        Equal(HookAction.Pass, state.Evaluate(binding, Keys.T, keyDown: true, keyUp: false, ctrl: false, alt: false, shift: true, win: true));
    }

    private static void SettingsChangeWhileHeldResets()
    {
        var state = new KeyboardHookState();
        var oldBinding = new HotkeyBinding(Keys.T, Ctrl: false, Alt: false, Shift: true, Win: true);
        var newBinding = new HotkeyBinding(Keys.Y, Ctrl: false, Alt: false, Shift: true, Win: true);

        Equal(HookAction.Trigger, state.Evaluate(oldBinding, Keys.T, keyDown: true, keyUp: false, ctrl: false, alt: false, shift: true, win: true));
        Equal(HookAction.Consume, state.Evaluate(newBinding, Keys.T, keyDown: false, keyUp: true, ctrl: false, alt: false, shift: false, win: false));
        Equal(HookAction.Trigger, state.Evaluate(newBinding, Keys.Y, keyDown: true, keyUp: false, ctrl: false, alt: false, shift: true, win: true));
    }

    private static void HotkeyRepeatsAfterModifierRelease()
    {
        var state = new KeyboardHookState();
        var binding = new HotkeyBinding(Keys.T, Ctrl: false, Alt: false, Shift: true, Win: true);
        Equal(HookAction.Trigger, state.Evaluate(binding, Keys.T, true, false, false, false, true, true));
        Equal(HookAction.Consume, state.Evaluate(binding, Keys.T, true, false, false, false, false, false));
        Equal(HookAction.Consume, state.Evaluate(binding, Keys.T, false, true, false, false, false, false));
        Equal(HookAction.Pass, state.Evaluate(binding, Keys.T, true, false, false, false, false, false));
    }

    private static void HotkeyRepeatsAfterSettingsChange()
    {
        var state = new KeyboardHookState();
        var oldBinding = new HotkeyBinding(Keys.T, Ctrl: false, Alt: false, Shift: true, Win: true);
        var newBinding = new HotkeyBinding(Keys.Y, Ctrl: false, Alt: false, Shift: true, Win: true);
        Equal(HookAction.Trigger, state.Evaluate(oldBinding, Keys.T, true, false, false, false, true, true));
        Equal(HookAction.Consume, state.Evaluate(newBinding, Keys.T, true, false, false, false, true, true));
        Equal(HookAction.Consume, state.Evaluate(newBinding, Keys.T, false, true, false, false, false, false));
        Equal(HookAction.Trigger, state.Evaluate(newBinding, Keys.Y, true, false, false, false, true, true));
    }

    private static void DifferentKeyWhileHotkeyHeldPasses()
    {
        var state = new KeyboardHookState();
        var oldBinding = new HotkeyBinding(Keys.T, Ctrl: false, Alt: false, Shift: true, Win: true);
        var newBinding = new HotkeyBinding(Keys.Y, Ctrl: false, Alt: false, Shift: true, Win: true);
        Equal(HookAction.Trigger, state.Evaluate(oldBinding, Keys.T, true, false, false, false, true, true));
        Equal(HookAction.Pass, state.Evaluate(newBinding, Keys.Y, true, false, false, false, true, true));
        Equal(HookAction.Pass, state.Evaluate(newBinding, Keys.Y, false, true, false, false, true, true));
        Equal(HookAction.Consume, state.Evaluate(newBinding, Keys.T, false, true, false, false, false, false));
        Equal(HookAction.Trigger, state.Evaluate(newBinding, Keys.Y, true, false, false, false, true, true));
    }

    private static void CopiedMessageIncludesPreview()
    {
        Strings.Use("en");
        string message = CaptureMessageFormatter.Format(CaptureOutcome.Copied("hello\r\nworld"));
        True(message.Contains("hello world", StringComparison.Ordinal));
    }

    private static void CopiedPreviewPreservesTextElements()
    {
        Strings.Use("en");
        string prefix = new string('a', 76);
        foreach (string element in new[] { "😀", "e\u0301", "👩‍💻" })
        {
            Equal($"Copied: {prefix}...",
                CaptureMessageFormatter.Format(CaptureOutcome.Copied(prefix + element + "suffix")));
        }
    }

    private static void NonCopiedMessagesAreDeterministic()
    {
        Strings.Use("en");
        Equal("No readable text found. Clipboard unchanged.", CaptureMessageFormatter.Format(CaptureOutcome.NoText));
        Equal("OCR found text, but clipboard did not change.", CaptureMessageFormatter.Format(CaptureOutcome.ClipboardUnchanged));
        Equal("Capture failed.", CaptureMessageFormatter.Format(CaptureOutcome.Error));
        Equal("Capture is already running.", CaptureMessageFormatter.Format(CaptureOutcome.Busy));
    }

    private static void ClipboardRetrySucceedsAfterTransientLock()
    {
        int calls = 0;
        int delays = 0;
        bool ok = ClipboardWriter.SetTextWithRetry(
            "text",
            value =>
            {
                calls++;
                if (calls < 3)
                    throw new ExternalException("clipboard locked");
            },
            () => calls >= 3 ? "text" : "",
            ms => delays++,
            attempts: 4);

        True(ok);
        Equal(3, calls);
        Equal(2, delays);
    }

    private static void ClipboardRetryRethrowsAfterFinalFailure()
    {
        int calls = 0;
        int delays = 0;
        try
        {
            ClipboardWriter.SetTextWithRetry(
                "text",
                value =>
                {
                    calls++;
                    throw new ExternalException("clipboard locked");
                },
                () => "",
                ms => delays++,
                attempts: 3);
        }
        catch (ExternalException)
        {
            Equal(3, calls);
            Equal(2, delays);
            return;
        }

        throw new InvalidOperationException("Expected final ExternalException.");
    }

    private static void ClipboardRetryDetectsUnchangedClipboard()
    {
        int calls = 0;
        int delays = 0;
        bool ok = ClipboardWriter.SetTextWithRetry(
            "new",
            value => calls++,
            () => "old",
            ms => delays++,
            attempts: 3);

        True(!ok);
        Equal(3, calls);
        Equal(2, delays);
    }

    private static void OcrPreprocessorScalesSmallSelections()
    {
        Equal(3, OcrImagePreprocessor.ScaleFor(400, 60));
        Equal(2, OcrImagePreprocessor.ScaleFor(900, 160));
        Equal(1, OcrImagePreprocessor.ScaleFor(2500, 1400));
    }

    private static void OcrPreprocessorBoundsLandscape()
    {
        using var source = new Bitmap(3300, 12);
        using var prepared = OcrImagePreprocessor.Prepare(source);
        Equal(new Size(3200, 11), prepared.Size);
        Equal(new Size(3300, 12), source.Size);
    }

    private static void OcrPreprocessorBoundsPortrait()
    {
        using var source = new Bitmap(12, 3300);
        using var prepared = OcrImagePreprocessor.Prepare(source);
        Equal(new Size(11, 3200), prepared.Size);
        Equal(new Size(12, 3300), source.Size);
    }

    private static void OcrScaleDoesNotOverflow()
    {
        Equal(1, OcrImagePreprocessor.ScaleFor(int.MaxValue, 60));
        Equal(1, OcrImagePreprocessor.ScaleFor(60, int.MaxValue));
    }

    private static void OcrPreprocessorRespectsEngineLimit()
    {
        using var source = new Bitmap(60, 20);
        using var prepared = OcrImagePreprocessor.Prepare(source, maxDimension: 32);
        Equal(new Size(32, 10), prepared.Size);
    }

    private static void AutoStartLeavesAbsentEntryAlone()
    {
        // No registry value means autostart is off. Startup must not switch it on by itself.
        Equal(
            AutoStartAction.LeaveAsIs,
            AutoStartDecision.EvaluateStartup(null, @"C:\Apps\WinTextCapture.exe", storedTargetExists: false));
    }

    private static void AutoStartRepairsMissingExecutable()
    {
        // The owner's real case: the value survives, the build it pointed at was cleaned away, so Windows
        // silently starts nothing. This is the one repair the app makes on its own.
        Equal(
            AutoStartAction.Write,
            AutoStartDecision.EvaluateStartup(@"""C:\Cleaned\WinTextCapture.exe""", @"C:\Apps\WinTextCapture.exe", storedTargetExists: false));
    }

    private static void AutoStartKeepsCurrentExecutable()
    {
        Equal(
            AutoStartAction.LeaveAsIs,
            AutoStartDecision.EvaluateStartup(@"""C:\Apps\WinTextCapture.exe""", @"C:\Apps\WinTextCapture.exe", storedTargetExists: true));
    }

    private static void AutoStartDoesNotHijackAnotherInstall()
    {
        // A different copy that still exists may have been chosen deliberately — repairing is not hijacking.
        Equal(
            AutoStartAction.LeaveAsIs,
            AutoStartDecision.EvaluateStartup(@"""D:\Installed\WinTextCapture.exe""", @"C:\Apps\WinTextCapture.exe", storedTargetExists: true));
    }

    private static void AutoStartHandlesQuotedAndUnquotedPaths()
    {
        Equal(
            AutoStartAction.LeaveAsIs,
            AutoStartDecision.EvaluateStartup(@"C:\Apps\WinTextCapture.exe", @"C:\Apps\WinTextCapture.exe", storedTargetExists: true));
        Equal(
            AutoStartAction.LeaveAsIs,
            AutoStartDecision.EvaluateStartup(@"""C:\Apps\WinTextCapture.exe""", @"C:\Apps\WinTextCapture.exe", storedTargetExists: true));
        Equal(@"C:\Apps\WinTextCapture.exe", AutoStartDecision.TargetPath(@"""C:\Apps\WinTextCapture.exe"""));
        Equal(@"C:\Apps\WinTextCapture.exe", AutoStartDecision.TargetPath(@"C:\Apps\WinTextCapture.exe"));
    }

    private static void AutoStartWritesWhenUserEnables()
    {
        Equal(
            AutoStartAction.Write,
            AutoStartDecision.EvaluateUserChange(true, null, @"C:\Apps\WinTextCapture.exe"));
        // Enabling from a copy other than the one already registered points the entry at this copy.
        Equal(
            AutoStartAction.Write,
            AutoStartDecision.EvaluateUserChange(true, @"""D:\Installed\WinTextCapture.exe""", @"C:\Apps\WinTextCapture.exe"));
    }

    private static void AutoStartRemovesEntryAfterUserDisable()
    {
        Equal(
            AutoStartAction.Remove,
            AutoStartDecision.EvaluateUserChange(false, @"""C:\Apps\WinTextCapture.exe""", @"C:\Apps\WinTextCapture.exe"));
    }

    private static void CrashLogFormatsHandledException()
    {
        var exception = new InvalidOperationException("original failure");
        string entry = CrashLog.Format(
            new DateTime(2026, 7, 25, 12, 34, 56, DateTimeKind.Utc),
            "1.0.2",
            fatal: false,
            exception);

        True(entry.Contains("2026-07-25T12:34:56.0000000Z", StringComparison.Ordinal));
        True(entry.Contains("WinTextCapture 1.0.2 | HANDLED", StringComparison.Ordinal));
        True(entry.Contains(exception.ToString(), StringComparison.Ordinal));
    }

    private static void CrashLogFormatsFatalException()
    {
        string entry = CrashLog.Format(
            DateTime.UnixEpoch,
            "1.0.2",
            fatal: true,
            new ApplicationException("fatal failure"));

        True(entry.Contains("| FATAL", StringComparison.Ordinal));
        True(entry.Contains("System.ApplicationException: fatal failure", StringComparison.Ordinal));
    }

    private static void ApplicationVersionIsStamped()
    {
        Equal("1.0.2", App.Version);
    }

    private static void SettingsNullPreferencesUseDefaults()
    {
        var settings = LoadSettingsFixture("{\"OcrLanguage\":null,\"Language\":null,\"Theme\":null}");
        Equal("auto", settings.OcrLanguage);
        Equal("auto", settings.Language);
        Equal("System", settings.Theme);
    }

    private static void SettingsMalformedOcrTagUsesDefault()
    {
        var settings = LoadSettingsFixture("{\"OcrLanguage\":\"not_a_language\",\"Theme\":\"Dark\"}");
        Equal("auto", settings.OcrLanguage);
        Equal("Dark", settings.Theme);
    }

    private static void SettingsInvalidShortcutUsesDefault()
    {
        var settings = LoadSettingsFixture("{\"HotKey\":0,\"ModCtrl\":false,\"ModAlt\":false,\"ModShift\":false,\"ModWin\":false,\"Theme\":\"Dark\"}");
        Equal(HotkeyBinding.FromSettings(new Settings()), HotkeyBinding.FromSettings(settings));
        Equal("Dark", settings.Theme);
    }

    private static void ModifierAndUndefinedKeysAreInvalid()
    {
        foreach (var key in new[] { Keys.ControlKey, Keys.LControlKey, Keys.RControlKey,
            Keys.ShiftKey, Keys.LShiftKey, Keys.RShiftKey, Keys.Menu, Keys.LMenu, Keys.RMenu,
            Keys.LWin, Keys.RWin, Keys.T | Keys.Control, (Keys)255 })
        {
            True(!new HotkeyBinding(key, Ctrl: true, Alt: false, Shift: false, Win: false).IsValid);
        }
    }

    private static void SettingsValidAndBrokenJson()
    {
        var settings = LoadSettingsFixture("{\"HotKey\":89,\"ModCtrl\":true,\"ModShift\":false,\"ModWin\":false,\"OcrLanguage\":\"tr-TR\",\"Language\":\"tr\",\"Theme\":\"Dark\"}");
        Equal(new HotkeyBinding(Keys.Y, true, false, false, false), HotkeyBinding.FromSettings(settings));
        Equal("tr-TR", settings.OcrLanguage);
        Equal("tr", settings.Language);
        Equal("Dark", settings.Theme);
        Equal("auto", LoadSettingsFixture("{").OcrLanguage);
        Equal("auto", LoadSettingsFixture("null").OcrLanguage);
        Equal("auto", LoadSettingsFixture("{}").OcrLanguage);
    }

    private static Settings LoadSettingsFixture(string json)
    {
        string path = Path.Combine(AppContext.BaseDirectory, $"settings-fixture-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, json);
            return Settings.Load(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void SettingsButtonsDoNotOverlapAppearance()
    {
        foreach (string language in new[] { "en", "tr" })
        {
            Strings.Use(language);
            using var form = new SettingsForm(new Settings(), autoStartEnabled: false);
            True(!form.Visible);
            foreach (var button in form.Controls.OfType<Button>())
            {
                True(form.ClientRectangle.Contains(button.Bounds));
                foreach (var combo in form.Controls.OfType<ComboBox>())
                {
                    if (button.Bounds.IntersectsWith(combo.Bounds))
                        throw new InvalidOperationException($"{language}: {button.Text} overlaps a dropdown at {combo.Bounds}.");
                }
            }
        }
        Strings.Use("en");
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            _passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL {name}");
            Console.Error.WriteLine(ex.Message);
            _failed++;
        }
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }

    private static void True(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("Expected condition to be true.");
    }
}
