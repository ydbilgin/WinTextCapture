using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinTextCapture;

internal static class Program
{
    private static int _passed;

    [STAThread]
    private static int Main()
    {
        Run("default hotkey triggers once, consumes repeats and key-up, then resets", HotkeyTriggersOnceAndResets);
        Run("wrong modifiers pass through", WrongModifiersPass);
        Run("invalid binding passes through", InvalidBindingPasses);
        Run("key-up resets even if settings changed while held", SettingsChangeWhileHeldResets);
        Run("capture messages include useful copied preview", CopiedMessageIncludesPreview);
        Run("capture messages expose no-text, error and busy states", NonCopiedMessagesAreDeterministic);
        Run("clipboard retry succeeds after transient lock", ClipboardRetrySucceedsAfterTransientLock);
        Run("clipboard retry rethrows after final failure", ClipboardRetryRethrowsAfterFinalFailure);
        Run("clipboard retry detects unchanged clipboard", ClipboardRetryDetectsUnchangedClipboard);
        Run("OCR preprocessing upscales small selections within bounds", OcrPreprocessorScalesSmallSelections);

        Console.WriteLine($"Passed: {_passed}");
        return 0;
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

    private static void CopiedMessageIncludesPreview()
    {
        Strings.Use("en");
        string message = CaptureMessageFormatter.Format(CaptureOutcome.Copied("hello\r\nworld"));
        True(message.Contains("hello world", StringComparison.Ordinal));
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
            Environment.Exit(1);
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
