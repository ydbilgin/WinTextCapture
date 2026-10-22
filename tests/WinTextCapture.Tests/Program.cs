using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Reflection;
using System.Threading.Tasks;
using WinTextCapture;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    [STAThread]
    private static int Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
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
        Run("modifier-only and out-of-range trigger keys are invalid", ModifierAndOutOfRangeKeysAreInvalid);
        Run("trigger keys without a named Keys value stay valid and survive load", UnnamedVirtualKeyStaysValid);
        Run("settings preserve valid preferences and recover broken JSON", SettingsValidAndBrokenJson);
        Run("settings action buttons do not overlap appearance controls", SettingsButtonsDoNotOverlapAppearance);
        Run("capture stays modal until asynchronous OCR finishes", CaptureStaysModalUntilOcrCompletes);
        Run("selection uses final release coordinates", SelectionUsesReleaseCoordinates);
        Run("right-button release does not finish a left drag", RightReleaseDoesNotFinishSelection);
        Run("selection is clipped to captured pixels", SelectionIsClipped);
        Run("unquoted startup commands with arguments preserve their executable", StartupCommandArguments);
        Run("failed partial settings writes preserve the previous file", SettingsWritesAreAtomic);
        Run("DPI changes keep screenshot and selection pixels aligned", CaptureDpiKeepsPhysicalBounds);
        Run("Turkish and RTL clipboard text stays exact", UnicodeClipboardRoundTrip);
        Run("selection supports reverse drags and ignores stray releases", ReverseAndStraySelection);
        Run("disposing pending OCR cannot report a late copy", DisposedCaptureCannotReportCopy);
        Run("English and Turkish string tables have the same keys", StringTablesHaveSameKeys);
        Run("settings controls sit on the 8 px grid with themed controls only", SettingsUsesGridAndThemedControls);
        Run("settings labels fit their English and Turkish text", SettingsLabelsFitText);
        Run("settings buttons take their colours from the theme", SettingsButtonsFollowTheme);
        Run("hotkey field shows key chips and updates from key presses", HotkeyFieldShowsChips);
        Run("hotkey field keeps Tab, Enter, Esc and Alt as dialog keys", HotkeyFieldKeyRouting);
        Run("menu hotkey text is the compact current combo", MenuHotkeyText);
        Run("tray menu keeps its items and actions and shows the hotkey", TrayMenuItemsAndHotkey);
        Run("tray menu renderer takes its colours from the theme", TrayMenuRendererColours);
        Run("OCR input is the clean screenshot crop without hint or outline pixels", OcrInputHasNoOverlayPixels);
        Run("overlay outline and size badge use the theme accent", OverlayOutlineUsesThemeAccent);
        Run("size badge stays on screen next to the selection", OverlayBadgePlacement);
        Run("hint pill sits at the top of the monitor under the cursor", OverlayHintPlacement);
        Run("toast content has a bold first line and detail for every outcome", ToastContentPerOutcome);
        Run("toast stripe is green for copied and amber otherwise, from the theme", ToastStripeColours);
        Run("toast keeps its 2.2 s timing and never activates", ToastTimingAndNoActivate);

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

    private static void ModifierAndOutOfRangeKeysAreInvalid()
    {
        foreach (var key in new[] { Keys.ControlKey, Keys.LControlKey, Keys.RControlKey,
            Keys.ShiftKey, Keys.LShiftKey, Keys.RShiftKey, Keys.Menu, Keys.LMenu, Keys.RMenu,
            Keys.LWin, Keys.RWin, Keys.T | Keys.Control, (Keys)255 })
        {
            True(!new HotkeyBinding(key, Ctrl: true, Alt: false, Shift: false, Win: false).IsValid);
        }
    }

    private static void UnnamedVirtualKeyStaysValid()
    {
        // 0xC1 has no named Keys value, but the hook compares raw virtual-key codes, so it must stay bindable.
        var binding = new HotkeyBinding((Keys)0xC1, Ctrl: true, Alt: false, Shift: false, Win: false);
        True(binding.IsValid);
        var state = new KeyboardHookState();
        Equal(HookAction.Trigger, state.Evaluate(binding, (Keys)0xC1, keyDown: true, keyUp: false, ctrl: true, alt: false, shift: false, win: false));

        var settings = LoadSettingsFixture("{\"HotKey\":193,\"ModCtrl\":true}");
        Equal(193, (int)settings.HotKey);
        True(settings.ModCtrl);
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

    private static void Mouse(OverlayForm form, string method, MouseButtons button, int x, int y) =>
        typeof(OverlayForm).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!
            .Invoke(form, new object?[] { null, new MouseEventArgs(button, 1, x, y, 0) });

    private static void CaptureStaysModalUntilOcrCompletes()
    {
        using var image = new Bitmap(160, 100);
        var completion = new TaskCompletionSource<CaptureOutcome>();
        using var form = new OverlayForm(new Settings(), image, _ => completion.Task);
        using var timer = new System.Windows.Forms.Timer { Interval = 80 };
        timer.Tick += (_, _) => { timer.Stop(); completion.TrySetResult(CaptureOutcome.NoText); };
        form.Shown += (_, _) => form.BeginInvoke((Action)(() =>
        {
            Mouse(form, "OnMouseDown", MouseButtons.Left, 10, 10);
            Mouse(form, "OnMouseMove", MouseButtons.Left, 90, 60);
            Mouse(form, "OnMouseUpAsync", MouseButtons.Left, 90, 60);
            timer.Start();
        }));
        var result = form.ShowDialog();
        bool waited = completion.Task.IsCompleted;
        completion.TrySetResult(CaptureOutcome.NoText);
        Application.DoEvents();
        True(waited);
        Equal(DialogResult.OK, result);
        Equal(CaptureStatus.NoText, form.Result.Status);
    }

    private static void SelectionUsesReleaseCoordinates()
    {
        using var image = new Bitmap(160, 100);
        Size? cropped = null;
        using var form = new OverlayForm(new Settings(), image, bitmap =>
        {
            cropped = bitmap.Size;
            return Task.FromResult(CaptureOutcome.NoText);
        });
        Mouse(form, "OnMouseDown", MouseButtons.Left, 10, 10);
        Mouse(form, "OnMouseMove", MouseButtons.Left, 40, 40);
        Mouse(form, "OnMouseUpAsync", MouseButtons.Left, 90, 60);
        Equal(new Size(80, 50), cropped);
    }

    private static void RightReleaseDoesNotFinishSelection()
    {
        using var image = new Bitmap(160, 100);
        int extracts = 0;
        using var form = new OverlayForm(new Settings(), image, _ =>
        {
            extracts++;
            return Task.FromResult(CaptureOutcome.NoText);
        });
        Mouse(form, "OnMouseDown", MouseButtons.Left, 10, 10);
        Mouse(form, "OnMouseMove", MouseButtons.Left, 80, 60);
        Mouse(form, "OnMouseUpAsync", MouseButtons.Right, 80, 60);
        Equal(0, extracts);
    }

    private static void SelectionIsClipped()
    {
        using var image = new Bitmap(160, 100);
        Size? cropped = null;
        using var form = new OverlayForm(new Settings(), image, bitmap =>
        {
            cropped = bitmap.Size;
            return Task.FromResult(CaptureOutcome.NoText);
        });
        Mouse(form, "OnMouseDown", MouseButtons.Left, 10, 10);
        Mouse(form, "OnMouseMove", MouseButtons.Left, 190, 130);
        Mouse(form, "OnMouseUpAsync", MouseButtons.Left, 190, 130);
        Equal(new Size(150, 90), cropped);
    }

    private static void StartupCommandArguments()
    {
        Equal(@"D:\Other install\WinTextCapture.exe", AutoStartDecision.TargetPath(@"D:\Other install\WinTextCapture.exe --quiet"));
        Equal(AutoStartAction.LeaveAsIs, AutoStartDecision.EvaluateStartup(@"D:\Other install\WinTextCapture.exe --quiet",
            @"C:\Current\WinTextCapture.exe", storedTargetExists: true));
    }

    private static void SettingsWritesAreAtomic()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "settings-audit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "settings.json");
        const string original = "{\"Theme\":\"Dark\"}";
        try
        {
            File.WriteAllText(path, original);
            var settings = new Settings { Theme = "Light" };
            True(!settings.Save(path, (target, _) =>
            {
                File.WriteAllText(target, "{");
                throw new IOException("Simulated partial disk write");
            }));
            Equal(original, File.ReadAllText(path));
            True(settings.Save(path));
            Equal("Light", Settings.Load(path).Theme);
            Equal(1, Directory.GetFiles(folder).Length);
        }
        finally { File.Delete(path); Directory.Delete(folder); }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    private static void CaptureDpiKeepsPhysicalBounds()
    {
        using var image = new Bitmap(160, 100);
        using var form = new OverlayForm(new Settings(), image, _ => Task.FromResult(CaptureOutcome.NoText));
        _ = form.Handle;
        var expected = form.Bounds;
        IntPtr suggested = Marshal.AllocHGlobal(16);
        try
        {
            Marshal.WriteInt32(suggested, 0, expected.Left);
            Marshal.WriteInt32(suggested, 4, expected.Top);
            Marshal.WriteInt32(suggested, 8, expected.Left + expected.Width * 2);
            Marshal.WriteInt32(suggested, 12, expected.Top + expected.Height * 2);
            SendMessage(form.Handle, 0x02E0, (IntPtr)((144 << 16) | 144), suggested);
            Equal(expected, form.Bounds);
        }
        finally { Marshal.FreeHGlobal(suggested); }
    }

    private static void UnicodeClipboardRoundTrip()
    {
        foreach (string text in new[] { "İı Şş Ğğ Çç Öö Üü\r\nTürkçe", "\u200Fمرحبا بالعالم\nשלום עולם" })
        {
            string? clipboard = null;
            True(ClipboardWriter.SetTextWithRetry(text, value => clipboard = value, () => clipboard!, _ => { }));
            Equal(text, clipboard);
            True(CaptureMessageFormatter.Format(CaptureOutcome.Copied(text)).Contains(text.Split('\n')[0].TrimEnd('\r'), StringComparison.Ordinal));
        }
    }

    private static void ReverseAndStraySelection()
    {
        using var image = new Bitmap(160, 100);
        Size? cropped = null;
        using var form = new OverlayForm(new Settings(), image, bitmap =>
        {
            cropped = bitmap.Size;
            return Task.FromResult(CaptureOutcome.NoText);
        });
        Mouse(form, "OnMouseUpAsync", MouseButtons.Left, 90, 60);
        Equal<Size?>(null, cropped);
        Mouse(form, "OnMouseDown", MouseButtons.Left, 90, 60);
        Mouse(form, "OnMouseUpAsync", MouseButtons.Left, 10, 10);
        Equal(new Size(80, 50), cropped);
    }

    private static void DisposedCaptureCannotReportCopy()
    {
        using var image = new Bitmap(160, 100);
        var completion = new TaskCompletionSource<CaptureOutcome>();
        using var form = new OverlayForm(new Settings(), image, _ => completion.Task);
        Mouse(form, "OnMouseDown", MouseButtons.Left, 10, 10);
        Mouse(form, "OnMouseUpAsync", MouseButtons.Left, 90, 60);
        form.Dispose();
        completion.SetResult(CaptureOutcome.Copied("late result"));
        Application.DoEvents();
        Equal(CaptureStatus.Cancelled, form.Result.Status);
    }

    private static void StringTablesHaveSameKeys()
    {
        Equal(0, Strings.MissingKeys().Count);
        foreach (string language in new[] { "en", "tr" })
        {
            Strings.Use(language);
            foreach (var property in typeof(Strings).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                string value = (string)property.GetValue(null)!;
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException($"{language}: {property.Name} has no text.");
            }
        }
        Strings.Use("en");
    }

    private static IEnumerable<Control> AllControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in AllControls(child)) yield return nested;
        }
    }

    private static void SettingsUsesGridAndThemedControls()
    {
        Strings.Use("en");
        using var form = new SettingsForm(new Settings { Theme = "Dark" }, autoStartEnabled: false);
        Equal(0, form.ClientSize.Width % 8);
        Equal(0, form.ClientSize.Height % 8);
        foreach (Control control in form.Controls)
        {
            if (control.Left % 8 != 0 || control.Top % 8 != 0)
                throw new InvalidOperationException($"{control.GetType().Name} '{control.Text}' is off the 8 px grid at {control.Location}.");
            if (control is TextBox || (control is Button && control is not ThemedButton)
                || (control is ComboBox && control is not ThemedComboBox) || (control is CheckBox && control is not ThemedCheckBox))
                throw new InvalidOperationException($"Unthemed {control.GetType().Name} '{control.Text}'.");
        }
        Equal(1, form.Controls.OfType<HotkeyField>().Count());
        Equal(3, form.Controls.OfType<ThemedComboBox>().Count());
        Equal(1, form.Controls.OfType<ThemedCheckBox>().Count());
    }

    private static void SettingsLabelsFitText()
    {
        foreach (string language in new[] { "en", "tr" })
        {
            Strings.Use(language);
            using var form = new SettingsForm(new Settings(), autoStartEnabled: false);
            foreach (var label in form.Controls.OfType<Label>())
            {
                var needed = TextRenderer.MeasureText(label.Text, label.Font, new Size(label.Width, 0), TextFormatFlags.WordBreak);
                if (needed.Height > label.Height || (label.Height <= 32 && needed.Width > label.Width))
                    throw new InvalidOperationException($"{language}: '{label.Text}' needs {needed}, has {label.Size}.");
            }
            var check = form.Controls.OfType<ThemedCheckBox>().Single();
            True(TextRenderer.MeasureText(check.Text, check.Font).Width + 32 <= check.Width);
        }
        Strings.Use("en");
    }

    private static void SettingsButtonsFollowTheme()
    {
        Strings.Use("en");
        foreach (var (pref, theme) in new[] { ("Dark", Theme.Dark), ("Light", Theme.Light) })
        {
            using var form = new SettingsForm(new Settings { Theme = pref }, autoStartEnabled: false);
            var buttons = form.Controls.OfType<ThemedButton>().ToList();
            Equal(theme.Accent, buttons.Single(b => b.Text == Strings.Save).FillColor);
            Equal(theme.Surface2, buttons.Single(b => b.Text == Strings.Cancel).FillColor);
            Equal(theme.Back, form.BackColor);
        }
    }

    private static void HotkeyFieldShowsChips()
    {
        Strings.Use("en");
        using var form = new SettingsForm(new Settings(), autoStartEnabled: false);
        var field = form.Controls.OfType<HotkeyField>().Single();
        Equal("Shift|Win|T", string.Join("|", field.Parts));
        Equal("Shift + Win + T", field.Text);

        var keyDown = typeof(SettingsForm).GetMethod("HotkeyBox_KeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!;
        keyDown.Invoke(form, new object?[] { field, new KeyEventArgs(Keys.Y | Keys.Control) });
        Equal("Ctrl|Y", string.Join("|", field.Parts));

        var clear = form.Controls.OfType<ThemedButton>().Single(b => b.Text == Strings.Clear);
        typeof(Button).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(clear, new object[] { EventArgs.Empty });
        Equal(0, field.Parts.Count);
        Equal(Strings.HotkeyPressKeys, field.Text);
    }

    private static void HotkeyFieldKeyRouting()
    {
        using var field = new HotkeyField();
        var isInputKey = typeof(HotkeyField).GetMethod("IsInputKey", BindingFlags.Instance | BindingFlags.NonPublic)!;
        bool Input(Keys keys) => (bool)isInputKey.Invoke(field, new object[] { keys })!;
        True(!Input(Keys.Tab));
        True(!Input(Keys.Tab | Keys.Shift));
        True(!Input(Keys.Enter));
        True(!Input(Keys.Escape));
        True(!Input(Keys.T | Keys.Alt));
        True(Input(Keys.T | Keys.Shift));
        True(Input(Keys.Left));
        True(Input(Keys.F5 | Keys.Control));
    }

    private static void MenuHotkeyText()
    {
        Equal("Shift+Win+T", new Settings().HotKeyShortText());
        Equal("Ctrl+Alt+F9", new Settings { HotKey = Keys.F9, ModCtrl = true, ModAlt = true, ModShift = false, ModWin = false }.HotKeyShortText());
    }

    private static void TrayMenuItemsAndHotkey()
    {
        var calls = new List<string>();
        var menu = new TrayMenu(() => calls.Add("capture"), () => calls.Add("settings"), () => calls.Add("about"), () => calls.Add("exit"));
        using var strip = menu.Strip;
        foreach (string language in new[] { "en", "tr" })
        {
            Strings.Use(language);
            menu.ApplyText(new Settings { HotKey = Keys.Y, ModCtrl = true, ModShift = false, ModWin = false });
            Equal(Strings.MenuCapture, menu.CaptureItem.Text);
            Equal("Ctrl+Y", menu.CaptureItem.ShortcutKeyDisplayString);
            True(menu.CaptureItem.ShowShortcutKeys);
            Equal(Keys.None, menu.CaptureItem.ShortcutKeys);   // display only; the global hook owns the key
        }
        Strings.Use("en");
        Equal("ToolStripMenuItem|ToolStripSeparator|ToolStripMenuItem|ToolStripMenuItem|ToolStripSeparator|ToolStripMenuItem",
            string.Join("|", strip.Items.Cast<ToolStripItem>().Select(i => i.GetType().Name)));
        foreach (ToolStripItem item in strip.Items)
            item.PerformClick();
        Equal("capture|settings|about|exit", string.Join("|", calls));
    }

    private static void TrayMenuRendererColours()
    {
        foreach (var theme in new[] { Theme.Dark, Theme.Light })
        {
            var colors = ThemedMenuRenderer.For(theme);
            Equal(theme.Surface, colors.Back);
            Equal(theme.Text, colors.Text);
            Equal(theme.Subtle, colors.Shortcut);
            Equal(theme.AccentWeak, colors.Hover);

            var menu = new TrayMenu(() => { }, () => { }, () => { }, () => { });
            using var strip = menu.Strip;
            menu.ApplyTheme(theme);
            using var bitmap = new Bitmap(120, 60);
            using (var g = Graphics.FromImage(bitmap))
            {
                var bounds = new Rectangle(0, 0, 120, 60);
                strip.Renderer.DrawToolStripBackground(new ToolStripRenderEventArgs(g, strip, bounds, Color.Empty));
                strip.Renderer.DrawToolStripBorder(new ToolStripRenderEventArgs(g, strip, bounds, Color.Empty));
            }
            Equal(theme.Surface.ToArgb(), bitmap.GetPixel(60, 30).ToArgb());
            Equal(theme.BorderStrong.ToArgb(), bitmap.GetPixel(0, 30).ToArgb());
        }
    }

    private static Bitmap PatternBitmap(int width, int height)
    {
        var bitmap = new Bitmap(width, height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                bitmap.SetPixel(x, y, Color.FromArgb(255, (x * 7 + y) % 256, (y * 5 + x * 3) % 256, (x ^ y) % 256));
        return bitmap;
    }

    private static Bitmap RenderOverlay(OverlayForm form, Size size)
    {
        var rendered = new Bitmap(size.Width, size.Height);
        using var g = Graphics.FromImage(rendered);
        form.PaintOverlay(g, new Rectangle(Point.Empty, size));
        return rendered;
    }

    private static void OcrInputHasNoOverlayPixels()
    {
        foreach (string theme in new[] { "Dark", "Light" })
        {
            using var source = PatternBitmap(400, 300);
            Bitmap? ocrInput = null;
            using var form = new OverlayForm(new Settings { Theme = theme }, source, bitmap =>
            {
                ocrInput = (Bitmap)bitmap.Clone();
                return Task.FromResult(CaptureOutcome.NoText);
            });
            form.HintArea = new Rectangle(0, 0, 400, 300);
            Mouse(form, "OnMouseDown", MouseButtons.Left, 20, 10);
            Mouse(form, "OnMouseMove", MouseButtons.Left, 380, 200);

            // The overlay really draws the hint pill, outline and badge inside the selected area ...
            var hint = form.HintBounds();
            True(new Rectangle(20, 10, 360, 190).Contains(hint));
            using (var rendered = RenderOverlay(form, source.Size))
            {
                var probe = new Point(hint.Left + 6, hint.Top + hint.Height / 2);
                True(rendered.GetPixel(probe.X, probe.Y).ToArgb() != source.GetPixel(probe.X, probe.Y).ToArgb());
                True(rendered.GetPixel(20, 100).ToArgb() != source.GetPixel(20, 100).ToArgb());
            }

            // ... but the bitmap handed to OCR is exactly the screenshot crop.
            Mouse(form, "OnMouseUpAsync", MouseButtons.Left, 380, 200);
            using var crop = ocrInput ?? throw new InvalidOperationException("OCR was not called.");
            Equal(new Size(360, 190), crop.Size);
            for (int y = 0; y < crop.Height; y++)
                for (int x = 0; x < crop.Width; x++)
                    if (crop.GetPixel(x, y).ToArgb() != source.GetPixel(x + 20, y + 10).ToArgb())
                        throw new InvalidOperationException($"{theme}: OCR pixel ({x},{y}) differs from the screenshot.");
        }
    }

    private static void OverlayOutlineUsesThemeAccent()
    {
        foreach (var (pref, theme) in new[] { ("Dark", Theme.Dark), ("Light", Theme.Light) })
        {
            using var source = new Bitmap(300, 200);
            using var form = new OverlayForm(new Settings { Theme = pref }, source, _ => Task.FromResult(CaptureOutcome.NoText));
            form.HintArea = new Rectangle(0, 0, 300, 200);
            form.PreviewSelection(new Rectangle(40, 80, 120, 50));
            using var rendered = RenderOverlay(form, source.Size);
            Equal(theme.Accent.ToArgb(), rendered.GetPixel(100, 80).ToArgb());
            var badge = form.BadgeBounds(new Rectangle(40, 80, 120, 50));
            Equal(theme.Accent.ToArgb(), rendered.GetPixel(badge.Left + 2, badge.Top + badge.Height / 2).ToArgb());
            Equal("120 \u00d7 50", OverlayForm.SizeText(new Rectangle(40, 80, 120, 50)));
        }
    }

    private static void OverlayBadgePlacement()
    {
        using var source = new Bitmap(300, 200);
        using var form = new OverlayForm(new Settings(), source, _ => Task.FromResult(CaptureOutcome.NoText));
        var client = new Rectangle(0, 0, 300, 200);
        var below = form.BadgeBounds(new Rectangle(40, 40, 100, 50));
        True(below.Top > 90 && client.Contains(below));
        var above = form.BadgeBounds(new Rectangle(40, 120, 100, 80));
        True(above.Bottom < 120 && client.Contains(above));
        var inside = form.BadgeBounds(new Rectangle(250, 0, 50, 200));
        True(client.Contains(inside));
        Equal(Rectangle.Empty, form.BadgeBounds(Rectangle.Empty));
    }

    private static void OverlayHintPlacement()
    {
        foreach (string language in new[] { "en", "tr" })
        {
            Strings.Use(language);
            using var source = new Bitmap(1200, 400);
            using var form = new OverlayForm(new Settings(), source, _ => Task.FromResult(CaptureOutcome.NoText));
            form.HintArea = new Rectangle(600, 0, 600, 400);   // the right-hand monitor
            var hint = form.HintBounds();
            True(form.HintArea.Contains(hint));
            True(Math.Abs((hint.Left + hint.Width / 2) - 900) <= 1);
            True(hint.Top < 60);
        }
        Strings.Use("en");
    }

    private static void ToastContentPerOutcome()
    {
        Strings.Use("en");
        var copied = CaptureMessageFormatter.Toast(CaptureOutcome.Copied("hello\r\nworld"))!;
        Equal("Copied", copied.Title);
        Equal("hello world", copied.Body);
        True(copied.Success);
        Equal("Text copied to clipboard.", CaptureMessageFormatter.Toast(CaptureOutcome.Copied(" "))!.Body);
        string prefix = new string('a', 76);
        Equal(prefix + "...", CaptureMessageFormatter.Toast(CaptureOutcome.Copied(prefix + "\U0001F600suffix"))!.Body);

        var noText = CaptureMessageFormatter.Toast(CaptureOutcome.NoText)!;
        Equal("No text found", noText.Title);
        Equal("Clipboard unchanged.", noText.Body);
        True(!noText.Success);
        Equal("Clipboard unchanged", CaptureMessageFormatter.Toast(CaptureOutcome.ClipboardUnchanged)!.Title);
        Equal("OCR found text, but clipboard did not change.", CaptureMessageFormatter.Toast(CaptureOutcome.ClipboardUnchanged)!.Body);
        Equal("Capture failed", CaptureMessageFormatter.Toast(CaptureOutcome.Error)!.Title);
        Equal("Capture already running", CaptureMessageFormatter.Toast(CaptureOutcome.Busy)!.Title);
        Equal<ToastContent?>(null, CaptureMessageFormatter.Toast(CaptureOutcome.Cancelled));

        Strings.Use("tr");
        Equal("Kopyaland\u0131", CaptureMessageFormatter.Toast(CaptureOutcome.Copied("x"))!.Title);
        Equal("Metin bulunamad\u0131", CaptureMessageFormatter.Toast(CaptureOutcome.NoText)!.Title);
        Strings.Use("en");
    }

    private static void ToastStripeColours()
    {
        foreach (var theme in new[] { Theme.Dark, Theme.Light })
        {
            using var copied = new ToastForm(new ToastContent("Copied", "preview", Success: true), theme);
            using var missing = new ToastForm(new ToastContent("No text found", "", Success: false), theme);
            True(!copied.Visible && !missing.Visible);
            Equal(theme.Success, copied.StripeColor);
            Equal(theme.Warning, missing.StripeColor);
            True(theme.Success != theme.Warning);

            using var bitmap = new Bitmap(copied.ClientSize.Width, copied.ClientSize.Height);
            using (var g = Graphics.FromImage(bitmap)) copied.PaintCard(g);
            Equal(theme.Success.ToArgb(), bitmap.GetPixel(2, bitmap.Height / 2).ToArgb());
            Equal(theme.Surface.ToArgb(), bitmap.GetPixel(bitmap.Width - 4, bitmap.Height / 2).ToArgb());
        }
    }

    private static void ToastTimingAndNoActivate()
    {
        using var toast = new ToastForm(new ToastContent("Copied", "preview", Success: true), Theme.Dark);
        var timer = (System.Windows.Forms.Timer)typeof(ToastForm).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(toast)!;
        Equal(2200, timer.Interval);
        True((bool)typeof(ToastForm).GetProperty("ShowWithoutActivation", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(toast)!);
        var cp = (CreateParams)typeof(ToastForm).GetProperty("CreateParams", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(toast)!;
        True((cp.ExStyle & 0x08000000) != 0);
        True(toast.TopMost);
        True(!toast.ShowInTaskbar);
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
            Console.Error.WriteLine(ex);
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
