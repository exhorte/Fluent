using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Fluent.Windows.Windowing;

public static class WindowActivationStyles
{
    private const int ExtendedStyleIndex = -20;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;

    public static void MakeNonActivatingToolWindow(nint windowHandle)
    {
        if (windowHandle == 0)
        {
            throw new ArgumentException("A valid window handle is required.", nameof(windowHandle));
        }

        nint style = NativeMethods.GetWindowLongPtr(windowHandle, ExtendedStyleIndex);
        nint newStyle = (nint)(style.ToInt64() | WsExNoActivate | WsExToolWindow);
        nint previous = NativeMethods.SetWindowLongPtr(windowHandle, ExtendedStyleIndex, newStyle);
        if (previous == 0)
        {
            int error = Marshal.GetLastWin32Error();
            if (error != 0)
            {
                throw new Win32Exception(error, "Could not apply non-activating window styles.");
            }
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        internal static extern nint GetWindowLongPtr(nint hWnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        internal static extern nint SetWindowLongPtr(nint hWnd, int index, nint value);
    }
}
