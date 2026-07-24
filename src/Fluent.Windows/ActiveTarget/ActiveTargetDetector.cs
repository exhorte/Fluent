using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using Fluent.Core.Interaction;

namespace Fluent.Windows.ActiveTarget;

public sealed class ActiveTargetDetector : IActiveTargetDetector
{
    public TargetSnapshot? CaptureActiveTarget()
    {
        nint foregroundWindow = NativeMethods.GetForegroundWindow();
        if (foregroundWindow == 0)
        {
            return null;
        }

        _ = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out int processId);

        AutomationElement? focusedElement = TryGetFocusedElement();
        string? runtimeId = TryGetRuntimeId(focusedElement);
        TargetSecurityStatus securityStatus = ClassifySecurityStatus(TryGetIsPassword(focusedElement));

        return new TargetSnapshot(
            WindowHandle: foregroundWindow.ToInt64(),
            ProcessId: processId,
            WindowTitle: GetWindowText(foregroundWindow),
            WindowClassName: GetClassName(foregroundWindow),
            FocusedElementRuntimeId: runtimeId,
            FocusedElementName: TryGetAutomationName(focusedElement),
            FocusedElementControlType: TryGetControlTypeName(focusedElement),
            IsPassword: securityStatus == TargetSecurityStatus.Password,
            CapturedAt: DateTimeOffset.UtcNow)
        {
            SecurityStatus = securityStatus
        };
    }

    internal static TargetSecurityStatus ClassifySecurityStatus(bool? isPassword)
    {
        return isPassword switch
        {
            true => TargetSecurityStatus.Password,
            false => TargetSecurityStatus.VerifiedNonPassword,
            null => TargetSecurityStatus.Unknown
        };
    }

    private static AutomationElement? TryGetFocusedElement()
    {
        try
        {
            return AutomationElement.FocusedElement;
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static string? TryGetRuntimeId(AutomationElement? element)
    {
        if (element is null)
        {
            return null;
        }

        try
        {
            return string.Join(".", element.GetRuntimeId());
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static bool? TryGetIsPassword(AutomationElement? element)
    {
        if (element is null)
        {
            return null;
        }

        try
        {
            object value = element.GetCurrentPropertyValue(
                AutomationElement.IsPasswordProperty,
                ignoreDefaultValue: true);

            return value is bool isPassword ? isPassword : null;
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static string? TryGetAutomationName(AutomationElement? element)
    {
        if (element is null)
        {
            return null;
        }

        try
        {
            return element.Current.Name;
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static string? TryGetControlTypeName(AutomationElement? element)
    {
        if (element is null)
        {
            return null;
        }

        try
        {
            return element.Current.ControlType.ProgrammaticName;
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static string GetWindowText(nint windowHandle)
    {
        int length = NativeMethods.GetWindowTextLength(windowHandle);
        if (length <= 0)
        {
            return string.Empty;
        }

        StringBuilder buffer = new(length + 1);
        _ = NativeMethods.GetWindowText(windowHandle, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    private static string GetClassName(nint windowHandle)
    {
        StringBuilder buffer = new(256);
        _ = NativeMethods.GetClassName(windowHandle, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern nint GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(nint hWnd, out int processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetWindowText(nint hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetWindowTextLength(nint hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetClassName(nint hWnd, StringBuilder className, int maxCount);
    }
}
