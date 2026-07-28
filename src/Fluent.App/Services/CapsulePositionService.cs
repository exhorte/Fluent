using System.Runtime.InteropServices;
using Fluent.App.Models;

namespace Fluent.App.Services;

/// <summary>
/// Default capsule positioning: center horizontally, near the bottom of the
/// work area of the monitor containing the foreground window or cursor.
/// Respects DPI scaling and taskbar edges.
/// </summary>
public sealed class CapsulePositionService : ICapsulePositionService
{
    public CapsulePosition GetPosition(double capsuleWidth, double capsuleHeight)
    {
        // 1. Determine the target monitor.
        nint monitor = GetTargetMonitor();

        // 2. Get the work area in physical pixels for that monitor.
        MonitorInfo monitorInfo = GetMonitorWorkArea(monitor);

        // 3. Get the DPI for that monitor.
        double dpiScale = GetMonitorDpiScale(monitor);

        // 4. Convert work area from physical pixels to DIPs.
        double workLeft = monitorInfo.Work.Left / dpiScale;
        double workTop = monitorInfo.Work.Top / dpiScale;
        double workWidth = (monitorInfo.Work.Right - monitorInfo.Work.Left) / dpiScale;
        double workHeight = (monitorInfo.Work.Bottom - monitorInfo.Work.Top) / dpiScale;

        // 5. Center horizontally, place near bottom with offset.
        double left = workLeft + (workWidth - capsuleWidth) / 2.0;
        double top = workTop + workHeight - capsuleHeight - CapsuleLayoutMetrics.BottomOffset;

        // 6. Clamp to work area bounds.
        if (left < workLeft)
        {
            left = workLeft;
        }

        if (top < workTop)
        {
            top = workTop;
        }

        if (left + capsuleWidth > workLeft + workWidth)
        {
            left = workLeft + workWidth - capsuleWidth;
        }

        if (top + capsuleHeight > workTop + workHeight)
        {
            top = workTop + workHeight - capsuleHeight - CapsuleLayoutMetrics.BottomOffset;
        }

        return new CapsulePosition(left, top);
    }

    // ── Monitor selection ──────────────────────────────────────────

    private static nint GetTargetMonitor()
    {
        // Priority: foreground window → cursor position → primary monitor.
        nint foreground = GetForegroundWindow();
        if (foreground != 0)
        {
            return MonitorFromWindow(foreground, MonitorDefaultTo.Nearest);
        }

        if (GetCursorPos(out Point pt))
        {
            return MonitorFromPoint(pt, MonitorDefaultTo.Nearest);
        }

        return MonitorFromPoint(new Point(0, 0), MonitorDefaultTo.Primary);
    }

    // ── Monitor info ───────────────────────────────────────────────

    private static MonitorInfo GetMonitorWorkArea(nint monitor)
    {
        MonitorInfoEx info = new() { Size = (uint)Marshal.SizeOf<MonitorInfoEx>() };
        if (!GetMonitorInfo(monitor, ref info))
        {
            // Fallback: use the primary monitor's work area.
            return new MonitorInfo
            {
                Work = new Rect
                {
                    Left = 0,
                    Top = 0,
                    Right = 1920,
                    Bottom = 1040
                }
            };
        }

        return new MonitorInfo
        {
            Work = new Rect
            {
                Left = info.Work.Left,
                Top = info.Work.Top,
                Right = info.Work.Right,
                Bottom = info.Work.Bottom
            }
        };
    }

    // ── DPI ────────────────────────────────────────────────────────

    private static double GetMonitorDpiScale(nint monitor)
    {
        // Use GetDpiForMonitor when available (Windows 10 1607+).
        // Fallback to 1.0 (96 DPI).
        try
        {
            int result = GetDpiForMonitor(
                monitor,
                DpiType.Effective,
                out uint dpiX,
                out uint _);
            if (result == 0 && dpiX > 0)
            {
                return dpiX / 96.0;
            }
        }
        catch
        {
            // GetDpiForMonitor not available — use 1.0.
        }

        return 1.0;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Native interop types & P/Invokes
    // ═══════════════════════════════════════════════════════════════

    private enum MonitorDefaultTo : uint
    {
        Nearest = 2,
        Primary = 1
    }

    private enum DpiType
    {
        Effective = 0
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
        public Point(int x, int y) { X = x; Y = y; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public uint Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    private struct MonitorInfo
    {
        public Rect Work;
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint hwnd, MonitorDefaultTo flags);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(Point pt, MonitorDefaultTo flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point lpPoint);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MonitorInfoEx lpmi);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(
        nint hMonitor,
        DpiType dpiType,
        out uint dpiX,
        out uint dpiY);
}
