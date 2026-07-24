using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Fluent.Windows.Hotkeys;

public sealed class GlobalHotKey : IDisposable
{
    public const int DefaultId = 0x4E59;

    private readonly nint _windowHandle;
    private readonly int _id;
    private bool _registered;

    public GlobalHotKey(nint windowHandle, int id = DefaultId)
    {
        if (windowHandle == 0)
        {
            throw new ArgumentException("A valid window handle is required.", nameof(windowHandle));
        }

        _windowHandle = windowHandle;
        _id = id;
    }

    public int Id => _id;

    public void RegisterCtrlSpace()
    {
        if (_registered)
        {
            return;
        }

        bool ok = NativeMethods.RegisterHotKey(_windowHandle, _id, HotKeyModifiers.Control | HotKeyModifiers.NoRepeat, (uint)ConsoleKey.Spacebar);
        if (!ok)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not register Ctrl+Space global hotkey.");
        }

        _registered = true;
    }

    public void Unregister()
    {
        if (!_registered)
        {
            return;
        }

        bool ok = NativeMethods.UnregisterHotKey(_windowHandle, _id);
        _registered = false;
        if (!ok)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not unregister global hotkey.");
        }
    }

    public void Dispose()
    {
        Unregister();
    }

    [Flags]
    private enum HotKeyModifiers : uint
    {
        Control = 0x0002,
        NoRepeat = 0x4000
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RegisterHotKey(nint hWnd, int id, HotKeyModifiers fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnregisterHotKey(nint hWnd, int id);
    }
}
