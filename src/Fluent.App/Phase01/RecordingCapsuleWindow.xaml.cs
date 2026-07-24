using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using Fluent.Windows.Windowing;

namespace Fluent.App.Phase01;

public partial class RecordingCapsuleWindow : Window
{
    private const int HitTestCaption = 2;
    private const int MouseActivateNoActivate = 3;
    private const int WmMouseActivate = 0x0021;
    private const int WmNcHitTest = 0x0084;

    private readonly Storyboard _processingPulseStoryboard;
    private readonly Storyboard _recordingWaveStoryboard;
    private HwndSource? _windowSource;
    private CapsuleVisualState _visualState = CapsuleVisualState.Unknown;

    public RecordingCapsuleWindow()
    {
        InitializeComponent();
        _recordingWaveStoryboard = (Storyboard)Resources["RecordingWaveStoryboard"];
        _processingPulseStoryboard = (Storyboard)Resources["ProcessingPulseStoryboard"];
        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
    }

    public void ShowIdleState()
    {
        if (_visualState == CapsuleVisualState.Idle)
        {
            return;
        }

        StopAnimations();
        ShowOnly(IdleContent);
        _visualState = CapsuleVisualState.Idle;
    }

    public void ShowRecordingState()
    {
        if (_visualState == CapsuleVisualState.Recording)
        {
            return;
        }

        StopAnimations();
        ShowOnly(RecordingContent);
        _recordingWaveStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
        _visualState = CapsuleVisualState.Recording;
    }

    public void ShowProcessingState(string message)
    {
        StatusText.Text = string.IsNullOrWhiteSpace(message) ? "Traitement…" : message;

        if (_visualState == CapsuleVisualState.Processing)
        {
            return;
        }

        StopAnimations();
        ShowOnly(ProcessingContent);
        _processingPulseStoryboard.Begin(this, HandoffBehavior.SnapshotAndReplace, isControllable: true);
        _visualState = CapsuleVisualState.Processing;
    }

    private void ShowOnly(UIElement visibleContent)
    {
        IdleContent.Visibility = ReferenceEquals(visibleContent, IdleContent)
            ? Visibility.Visible
            : Visibility.Collapsed;
        RecordingContent.Visibility = ReferenceEquals(visibleContent, RecordingContent)
            ? Visibility.Visible
            : Visibility.Collapsed;
        ProcessingContent.Visibility = ReferenceEquals(visibleContent, ProcessingContent)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void StopAnimations()
    {
        _recordingWaveStoryboard.Remove(this);
        _processingPulseStoryboard.Remove(this);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _windowSource?.RemoveHook(WindowProc);
        _windowSource = null;
        StopAnimations();
        _visualState = CapsuleVisualState.Unknown;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        nint handle = new WindowInteropHelper(this).Handle;
        WindowActivationStyles.MakeNonActivatingToolWindow(handle);
        _windowSource = HwndSource.FromHwnd(handle);
        _windowSource?.AddHook(WindowProc);
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

        if (msg == WmNcHitTest)
        {
            handled = true;
            return (nint)HitTestCaption;
        }

        return 0;
    }

    private enum CapsuleVisualState
    {
        Unknown,
        Idle,
        Recording,
        Processing
    }
}
