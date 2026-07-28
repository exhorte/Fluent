using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Fluent.Windows.Hotkeys;

/// <summary>
/// Low-level keyboard hook (WH_KEYBOARD_LL) that detects the Ctrl+Win
/// modifier-only combination used for Push-to-Talk.
///
/// Design:
/// - The hook callback runs on the thread that installed the hook (the
///   calling thread's message pump). In a WPF application, this is the
///   UI thread.
/// - The callback is kept minimal: it updates key state and fires events.
/// - Key-repeat messages (lParam bit 30) are ignored for activation.
/// - Left/Right variants of Ctrl and Win are treated identically.
/// - Win key alone is suppressed during an active PTT sequence to prevent
///   the Start Menu from opening.
/// </summary>
public sealed class GlobalPushToTalkHook : IGlobalPushToTalkHook
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;

    private const ushort VkLeftCtrl = 0xA2;
    private const ushort VkRightCtrl = 0xA3;
    private const ushort VkLeftWin = 0x5B;
    private const ushort VkRightWin = 0x5C;

    private readonly object _sync = new();
    private HookProc? _callback;
    private nint _hookId;
    private bool _disposed;

    // Per-cycle state (reset after each complete deactivation).
    private bool _ctrlDown;
    private bool _winDown;
    private bool _startFired;
    private bool _stopFired;
    private bool _activeSequence;

    public event EventHandler? StartRequested;
    public event EventHandler? StopRequested;

    public bool IsInstalled
    {
        get { lock (_sync) return _hookId != 0; }
    }

    public void Install()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(GlobalPushToTalkHook));
            }

            if (_hookId != 0)
            {
                return; // already installed
            }

            _callback = HookCallback;
        }

        // SetWindowsHookEx must be called on the thread that owns the
        // message pump (the UI thread). The module handle is IntPtr.Zero
        // for a thread-specific low-level hook.
        nint hook = NativeMethods.SetWindowsHookEx(
            WhKeyboardLl,
            _callback,
            nint.Zero,
            0);

        if (hook == 0)
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Could not install WH_KEYBOARD_LL hook for Push-to-Talk.");
        }

        lock (_sync)
        {
            _hookId = hook;
        }
    }

    public void Uninstall()
    {
        nint hook;
        lock (_sync)
        {
            if (_hookId == 0)
            {
                return;
            }

            hook = _hookId;
            _hookId = 0;
            _activeSequence = false;
            _ctrlDown = false;
            _winDown = false;
            _startFired = false;
            _stopFired = false;
        }

        bool ok = NativeMethods.UnhookWindowsHookEx(hook);
        if (!ok)
        {
            int error = Marshal.GetLastWin32Error();
            Debug.WriteLine(
                $"PushToTalk: UnhookWindowsHookEx failed (error {error}). " +
                "The hook may already have been released.");
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        Uninstall();
        _callback = null;
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            ProcessKeyEvent((int)wParam, lParam);
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void ProcessKeyEvent(int msg, nint lParam)
    {
        ushort vk = (ushort)Marshal.ReadInt32(lParam);
        bool isKeyDown = msg is WmKeyDown or WmSysKeyDown;
        bool isKeyUp = msg is WmKeyUp or WmSysKeyUp;

        // Read the repeat flag from bit 30 of lParam.
        bool isRepeat = false;
        if (isKeyDown)
        {
            // The repeat flag is bit 30 of the KBDLLHOOKSTRUCT flags field,
            // which is at offset 8 in the structure (after vkCode and scanCode).
            uint flags = (uint)Marshal.ReadInt32(lParam, 8);
            isRepeat = (flags & 0x40000000) != 0;
        }

        bool isCtrl = vk is VkLeftCtrl or VkRightCtrl;
        bool isWin = vk is VkLeftWin or VkRightWin;

        if (!isCtrl && !isWin)
        {
            return; // not a key we track
        }

        lock (_sync)
        {
            if (_disposed || _hookId == 0)
            {
                return;
            }

            if (isKeyDown && isCtrl && !isRepeat)
            {
                _ctrlDown = true;
            }
            else if (isKeyUp && isCtrl)
            {
                _ctrlDown = false;
            }
            else if (isKeyDown && isWin && !isRepeat)
            {
                _winDown = true;
            }
            else if (isKeyUp && isWin)
            {
                _winDown = false;
            }

            bool bothDown = _ctrlDown && _winDown;

            // ── Start: transition from not-both to both ─────────
            if (bothDown && !_startFired)
            {
                _startFired = true;
                _stopFired = false;
                _activeSequence = true;
                OnStartRequested();
                return;
            }

            // ── Stop: either key released while active ─────────
            if (!bothDown && _activeSequence && !_stopFired)
            {
                _stopFired = true;
                OnStopRequested();

                // Reset per-cycle state for the next activation.
                _startFired = false;
                _activeSequence = false;
                _ctrlDown = false;
                _winDown = false;
                return;
            }
        }
    }

    /// <summary>
    /// Returns true if the Win key event should be suppressed (eaten by
    /// the hook) to prevent the Start Menu from opening during an active
    /// Push-to-Talk sequence.
    /// </summary>
    public bool ShouldSuppressWinKey(int msg, nint lParam)
    {
        if (lParam == 0)
        {
            return false;
        }

        ushort vk;
        try
        {
            vk = (ushort)Marshal.ReadInt32(lParam);
        }
        catch (AccessViolationException)
        {
            return false;
        }

        bool isWin = vk is VkLeftWin or VkRightWin;

        if (!isWin)
        {
            return false;
        }

        lock (_sync)
        {
            return _activeSequence;
        }
    }

    private void OnStartRequested()
    {
        EventHandler? handler = StartRequested;
        handler?.Invoke(this, EventArgs.Empty);
    }

    private void OnStopRequested()
    {
        EventHandler? handler = StopRequested;
        handler?.Invoke(this, EventArgs.Empty);
    }

    private delegate nint HookProc(int nCode, nint wParam, nint lParam);

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern nint SetWindowsHookEx(
            int idHook,
            HookProc lpfn,
            nint hMod,
            uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(nint hhk);

        [DllImport("user32.dll")]
        internal static extern nint CallNextHookEx(
            nint hhk,
            int nCode,
            nint wParam,
            nint lParam);
    }
}
