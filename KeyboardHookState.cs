namespace WinTextCapture;

internal enum HookAction
{
    Pass,
    Consume,
    Trigger
}

internal sealed class KeyboardHookState
{
    private Keys _activeKey = Keys.None;

    public HookAction Evaluate(
        HotkeyBinding binding,
        Keys key,
        bool keyDown,
        bool keyUp,
        bool ctrl,
        bool alt,
        bool shift,
        bool win)
    {
        if (!keyDown && !keyUp)
            return HookAction.Pass;

        if (keyUp)
        {
            if (_activeKey != Keys.None && key == _activeKey)
            {
                _activeKey = Keys.None;
                return HookAction.Consume;
            }

            return HookAction.Pass;
        }

        if (!binding.Matches(key, ctrl, alt, shift, win))
            return HookAction.Pass;

        if (_activeKey != Keys.None)
            return HookAction.Consume;

        _activeKey = key;
        return HookAction.Trigger;
    }
}
