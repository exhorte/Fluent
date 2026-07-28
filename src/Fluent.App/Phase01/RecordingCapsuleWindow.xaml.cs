using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using Fluent.App.Models;
using Fluent.Windows.Windowing;

namespace Fluent.App.Phase01;

public partial class RecordingCapsuleWindow : Window
{
    private const int MouseActivateNoActivate = 3;
    private const int WmMouseActivate = 0x0021;

    private const int WsExTransparent = 0x00000020;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;
    private const int ExtendedStyleIndex = -20;

    private readonly Storyboard _recordingWaveStoryboard;
    private readonly Storyboard _processingPulseStoryboard;
    private HwndSource? _windowSource;
    private CapsuleVisualState _visualState = CapsuleVisualState.Idle;

    /// <summary>Future cancel action (not wired in this batch).</summary>
    public ICommand? CancelCommand { get; set; }

    /// <summary>Future paste action (not wired in this batch).</summary>
    public ICommand? PasteCommand { get; set; }

    public RecordingCapsuleWindow()
    {
        InitializeComponent();
        _recordingWaveStoryboard = (Storyboard)Resources["RecordingWaveStoryboard"];
        _processingPulseStoryboard = (Storyboard)Resources["ProcessingPulseStoryboard"];

        // Wire future commands — safe no-ops until assigned.
        CancelButton.Command = new RelayCommand(
            () =>
            {
                if (CancelCommand?.CanExecute(null) == true)
                {
                    CancelCommand.Execute(null);
                }
            },
            () => CancelCommand?.CanExecute(null) == true);

        PasteButton.Command = new RelayCommand(
            () =>
            {
                if (PasteCommand?.CanExecute(null) == true)
                {
                    PasteCommand.Execute(null);
                }
            },
            () => PasteCommand?.CanExecute(null) == true);

        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
    }

    public CapsuleVisualState VisualState => _visualState;

    // ── Public state transitions ───────────────────────────────────

    public void ShowIdleState()
    {
        if (_visualState == CapsuleVisualState.Idle)
        {
            return;
        }

        StopAnimations();
        ShowOnly(IdleContent);
        EnableClickThrough(true);
        _visualState = CapsuleVisualState.Idle;
    }

    public void ShowRecordingState()
    {
        if (_visualState == CapsuleVisualState.Recording)
        {
            return;
        }

        StopAnimations();
        ShowOnly(ActiveContent);
        EnableClickThrough(false);
        _recordingWaveStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
        _visualState = CapsuleVisualState.Recording;
    }

    public void ShowProcessingState()
    {
        if (_visualState == CapsuleVisualState.Processing)
        {
            return;
        }

        StopAnimations();
        ShowOnly(ActiveContent);
        EnableClickThrough(false);
        _processingPulseStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
        _visualState = CapsuleVisualState.Processing;
    }

    public void ShowErrorState()
    {
        // Error returns directly to idle after a clean stop.
        StopAnimations();
        ShowIdleState();
    }

    // ── Visibility toggle ──────────────────────────────────────────

    private void ShowOnly(UIElement visibleContent)
    {
        IdleContent.Visibility = ReferenceEquals(visibleContent, IdleContent)
            ? Visibility.Visible
            : Visibility.Collapsed;
        ActiveContent.Visibility = ReferenceEquals(visibleContent, ActiveContent)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    // ── Animation control ───────────────────────────────────────────

    private void StopAnimations()
    {
        _recordingWaveStoryboard.Remove(this);
        _processingPulseStoryboard.Remove(this);
    }

    // ── Click-through control (idle only) ───────────────────────────

    private void EnableClickThrough(bool enable)
    {
        if (_windowSource?.Handle is { } h && h != 0)
        {
            nint style = NativeMethods.GetWindowLongPtr(h, ExtendedStyleIndex);
            nint newStyle = enable
                ? (nint)(style.ToInt64() | WsExTransparent)
                : (nint)(style.ToInt64() & ~WsExTransparent);
            // Always keep NO_ACTIVATE and TOOLWINDOW.
            newStyle = (nint)(newStyle.ToInt64() | WsExNoActivate | WsExToolWindow);
            NativeMethods.SetWindowLongPtr(h, ExtendedStyleIndex, newStyle);
        }
    }

    // ── Window initialization & cleanup ─────────────────────────────

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        nint handle = new WindowInteropHelper(this).Handle;
        WindowActivationStyles.MakeNonActivatingToolWindow(handle);
        _windowSource = HwndSource.FromHwnd(handle);
        _windowSource?.AddHook(WindowProc);

        // Start click-through in idle state.
        EnableClickThrough(true);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _windowSource?.RemoveHook(WindowProc);
        _windowSource = null;
        StopAnimations();
        _visualState = CapsuleVisualState.Idle;
    }

    private static nint WindowProc(
        nint hwnd,
        int msg,
        nint wParam,
        nint lParam,
        ref bool handled)
    {
        if (msg == WmMouseActivate)
        {
            handled = true;
            return (nint)MouseActivateNoActivate;
        }

        return 0;
    }

    // ── Native interop ─────────────────────────────────────────────

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        internal static extern nint GetWindowLongPtr(nint hWnd, int index);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        internal static extern nint SetWindowLongPtr(nint hWnd, int index, nint value);
    }

    /// <summary>Minimal ICommand for future wiring; no external dependency.</summary>
    private sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute();

        public void Execute(object? parameter) => _execute();

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
