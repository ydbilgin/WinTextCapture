using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinTextCapture;

/// <summary>Consumes the capture hotkey before Windows or PowerToys can handle it.</summary>
internal sealed class KeyboardHook : IDisposable
{
    private readonly Func<HotkeyBinding> _binding;
    private readonly Action _pressed;
    private readonly Native.HookProc _proc;
    private readonly KeyboardHookState _state = new();
    private IntPtr _hook = IntPtr.Zero;

    public KeyboardHook(Func<HotkeyBinding> binding, Action pressed)
    {
        _binding = binding;
        _pressed = pressed;
        _proc = HookCallback;
    }

    public bool Start()
    {
        if (_hook != IntPtr.Zero) return true;

        using var curProcess = Process.GetCurrentProcess();
        _hook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _proc, Native.GetModuleHandle(null), 0);
        return _hook != IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return Native.CallNextHookEx(_hook, nCode, wParam, lParam);

        bool keyDown = wParam == (IntPtr)Native.WM_KEYDOWN || wParam == (IntPtr)Native.WM_SYSKEYDOWN;
        bool keyUp = wParam == (IntPtr)Native.WM_KEYUP || wParam == (IntPtr)Native.WM_SYSKEYUP;
        if (!keyDown && !keyUp)
            return Native.CallNextHookEx(_hook, nCode, wParam, lParam);

        var data = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
        if (data.dwExtraInfo == Native.InjectedTag)
            return Native.CallNextHookEx(_hook, nCode, wParam, lParam);

        var key = (Keys)data.vkCode;
        bool ctrl = Native.IsDown(Native.VK_CONTROL);
        bool alt = Native.IsDown(Native.VK_MENU);
        bool shift = Native.IsDown(Native.VK_SHIFT);
        bool win = Native.IsDown(Native.VK_LWIN) || Native.IsDown(Native.VK_RWIN);
        var binding = _binding();
        HookAction action = _state.Evaluate(binding, key, keyDown, keyUp, ctrl, alt, shift, win);
        if (action == HookAction.Pass)
            return Native.CallNextHookEx(_hook, nCode, wParam, lParam);

        if (action == HookAction.Trigger && win)
            Native.MaskWinKey();

        if (action == HookAction.Trigger)
        {
            _pressed();
        }

        return (IntPtr)1;
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            Native.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
