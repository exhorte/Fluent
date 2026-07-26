using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Fluent.App.Auth;
using Fluent.App.Cloud;
using Fluent.App.Dashboard;
using Fluent.App.Phase01;
using Fluent.Audio.Capture;
using Fluent.App.Localization;
using Fluent.Core.Transcription;
using Fluent.Core.Diagnostics;
using Fluent.Core.History;
using Fluent.Core.Interaction;
using Fluent.Core.Settings;
using Fluent.Persistence.Dictionary;
using Fluent.Persistence.History;
using Fluent.Persistence.Settings;
using Fluent.Rewrite;
using Fluent.Rewrite.Dictionary;
using Fluent.Rewrite.Observability;
using Fluent.Rewrite.Orchestration;
using Fluent.Rewrite.Profiles;
using Fluent.Rewrite.Providers;
using Fluent.Rewrite.Rewriting;
using Fluent.Rewrite.Validation;
using Fluent.Speech.Transcription;
using Fluent.Windows.ActiveTarget;
using Fluent.Windows.Hotkeys;
using Fluent.Windows.Input;

namespace Fluent.App;

public partial class MainWindow : Window
{
    private const int WmHotKey = 0x0312;
    private static readonly TimeSpan MinimumRecordingDuration = TimeSpan.FromMilliseconds(250);

    private readonly ActiveTargetDetector _targetDetector = new();
    private readonly KeyboardInputSender _keyboardInputSender = new();
    private readonly TextInsertionPolicy _textInsertionPolicy = new();
    private readonly IAudioRecorder _audioRecorder = new InMemoryMicrophoneRecorder();
    private readonly ISpeechTranscriber _speechTranscriber = new WhisperFrenchTranscriber();
    private readonly SafeProfileRewriteService _profileRewriteService = new(
        new ProfileRoutedRewriter(),
        new RewriteOutputValidator());
    private readonly ProfileSelection _profileSelection = new();
    private readonly SupabaseAuthenticationState _authenticationState;
    private readonly CloudRewriteSettings _cloudRewriteSettings = new();
    private readonly FluentBackendPublicConfiguration? _cloudBackendConfiguration;
    private readonly string _cloudBackendUnavailableReason;
    private readonly HttpClient _cloudHttpClient = new();
    private readonly RewriteOrchestrator _rewriteOrchestrator;
    private readonly PersistentPersonalDictionary _personalDictionary = new(
        new SqlitePersonalDictionaryStore());
    private readonly IDictationHistoryStore _dictationHistoryStore =
        new SqliteDictationHistoryStore();
    private readonly IAppSettingsStore _appSettingsStore =
        new SqliteAppSettingsStore();
    private readonly PersonalDictionaryProcessor _dictionaryProcessor = new();
    private readonly Localizer _localizer =
        (Localizer)Application.Current.Resources["Loc"];
    private int _historyEntryCount;
    private readonly CancellationTokenSource _shutdown = new();
    private GlobalHotKey? _hotKey;
    private HwndSource? _source;
    private RecordingCapsuleWindow? _capsule;
    private TargetSnapshot? _lockedTarget;
    private RewriteProfile? _activeDictationProfile;
    private TranscriptionLanguageMode _currentTranscriptionLanguageMode =
        TranscriptionLanguageMode.Auto;
    private TranscriptionLanguageMode _activeTranscriptionLanguageMode =
        TranscriptionLanguageMode.Auto;
    private TranscriptionLanguage? _lastDetectedLanguage;
    private TranscriptionLanguage _resolvedDictationLanguage =
        TranscriptionLanguage.French;
    private DictationState _state;
    private DictationFailureStage _dictationStage = DictationFailureStage.Unknown;
    private bool _isBusy;
    private bool _isClosing;

    public MainWindow()
    {
        _authenticationState = SupabaseAuthenticationState.CreateDefault(_cloudHttpClient);
        FluentBackendPublicConfiguration.TryLoadFromEnvironment(
            out FluentBackendPublicConfiguration? cloudBackendConfiguration,
            out _cloudBackendUnavailableReason);
        _cloudBackendConfiguration = cloudBackendConfiguration;
        _rewriteOrchestrator = new RewriteOrchestrator(
            new LocalRewriteProvider(_profileRewriteService),
            new CloudRewriteProvider(
                new GeminiRewriteProvider(
                    new Fluent.Cloud.BackendCloudRewriteClient(
                        _cloudHttpClient,
                        BuildCloudBackendOptions)),
                new DeepSeekRewriteProvider(
                    new Fluent.Cloud.BackendCloudRewriteClient(
                        _cloudHttpClient,
                        BuildCloudBackendOptions))),
            new CloudRewriteValidator());

        InitializeComponent();
        DictionaryPage.EntryCountChanged += OnDictionaryEntryCountChanged;
        DictionaryPage.StorageModeChanged += OnDictionaryStorageModeChanged;
        DictionaryPage.Initialize(_personalDictionary, _shutdown.Token);
        HistoryPage.EntryCountChanged += OnHistoryEntryCountChanged;
        HistoryPage.EnabledChanged += OnHistoryEnabledChanged;
        HistoryPage.Initialize(_dictationHistoryStore, _shutdown.Token);
        SettingsPage.DefaultProfileChangeRequested += OnSettingsDefaultProfileChangeRequested;
        SettingsPage.OpenHistoryRequested += OnSettingsOpenHistoryRequested;
        SettingsPage.LanguageChangeRequested += OnLanguageChangeRequested;
        SettingsPage.TranscriptionLanguageChangeRequested +=
            OnTranscriptionLanguageChangeRequested;
        SettingsPage.SetProfiles(RewriteProfiles.All, _profileSelection.Current);
        ProfilesPage.SelectionChanged += OnProfileSelectionChanged;
        ProfilesPage.CloudStateChanged += OnCloudStateChanged;
        _authenticationState.Changed += OnAuthenticationStateChanged;
        ProfilesPage.Initialize(_profileSelection);
        ProfilesPage.InitializeCloud(
            _cloudRewriteSettings,
            _authenticationState,
            _cloudBackendConfiguration is not null,
            _cloudBackendUnavailableReason);
        UpdateProfilePresentation(_profileSelection.Current);
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        nint handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);

        // Dark grey title bar via DWM.
        DarkenTitleBar(handle);

        // Set the window icon from the local asset.
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Fluent.ico");
        if (File.Exists(iconPath))
        {
            Icon = new System.Windows.Media.Imaging.BitmapImage(
                new Uri(iconPath, UriKind.Absolute));
        }

        try
        {
            await DictionaryPage.LoadAsync();
            await HistoryPage.LoadAsync();
            await RestorePreferencesAsync();
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            return;
        }

        if (_isClosing)
        {
            return;
        }

        try
        {
            await _authenticationState.RestoreSessionAsync(_shutdown.Token);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            return;
        }

        if (_isClosing)
        {
            return;
        }

        UpdateUserProfileChip();

        try
        {
            _hotKey = new GlobalHotKey(handle);
            _hotKey.RegisterCtrlSpace();
            HotkeyStatusText.Text = _localizer["dictation.status.active"];
            DictationStateText.Text = _localizer["dictation.status.ready"];
            ShowIdleCapsule();
        }
        catch (Exception ex)
        {
            HotkeyStatusText.Text = _localizer["dictation.status.unavailable"];
            LastResultText.Text = ex.Message;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _isClosing = true;
        _shutdown.Cancel();
        _source?.RemoveHook(WndProc);
        DictionaryPage.EntryCountChanged -= OnDictionaryEntryCountChanged;
        DictionaryPage.StorageModeChanged -= OnDictionaryStorageModeChanged;
        HistoryPage.EntryCountChanged -= OnHistoryEntryCountChanged;
        HistoryPage.EnabledChanged -= OnHistoryEnabledChanged;
        SettingsPage.DefaultProfileChangeRequested -= OnSettingsDefaultProfileChangeRequested;
        SettingsPage.OpenHistoryRequested -= OnSettingsOpenHistoryRequested;
        SettingsPage.LanguageChangeRequested -= OnLanguageChangeRequested;
        SettingsPage.TranscriptionLanguageChangeRequested -=
            OnTranscriptionLanguageChangeRequested;
        ProfilesPage.SelectionChanged -= OnProfileSelectionChanged;
        ProfilesPage.CloudStateChanged -= OnCloudStateChanged;
        _authenticationState.Changed -= OnAuthenticationStateChanged;
        _hotKey?.Dispose();
        _capsule?.Close();
        _audioRecorder.Dispose();
        _speechTranscriber.Dispose();
        _authenticationState.Dispose();
        _cloudHttpClient.Dispose();
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmHotKey && _hotKey is not null && wParam.ToInt32() == _hotKey.Id)
        {
            handled = true;
            HandleHotKey();
        }

        return 0;
    }

    private async void HandleHotKey()
    {
        if (_isBusy)
        {
            LastResultText.Text = _localizer["dictation.busy"];
            return;
        }

        try
        {
            if (_state == DictationState.Idle)
            {
                StartRecording();
                return;
            }

            if (_state == DictationState.Recording)
            {
                await CompleteDictationAsync();
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            ResetToIdle();
        }
        catch (Exception ex)
        {
            DictationFailureStage stage = _dictationStage;
            ResetToIdle();
            if (_isClosing)
            {
                return;
            }

            UserFacingMessage message = DictationErrorPresenter.Describe(stage);
            DictationStateText.Text = _localizer["dictation.status.failed"];
            LastResultText.Text = message.Combined;
            // Technical detail stays in the debug log only, never in the UI.
            Debug.WriteLine($"Dictation failure ({stage}): {ex}");
        }
    }

    private void StartRecording()
    {
        _lockedTarget = _targetDetector.CaptureActiveTarget();
        if (_lockedTarget is null)
        {
            DictationStateText.Text = _localizer["dictation.status.blocked"];
            LastResultText.Text = _localizer["dictation.guard.noTarget"];
            return;
        }

        if (_lockedTarget.IsPasswordTarget)
        {
            DictationStateText.Text = _localizer["dictation.status.blocked"];
            TargetStatusText.Text = DescribeTarget(_lockedTarget);
            LastResultText.Text = _localizer["dictation.guard.passwordBlocked"];
            _lockedTarget = null;
            return;
        }

        if (!_lockedTarget.IsUsable)
        {
            DictationStateText.Text = _localizer["dictation.status.blocked"];
            TargetStatusText.Text = DescribeTarget(_lockedTarget);
            LastResultText.Text = _localizer["dictation.guard.unverifiable"];
            _lockedTarget = null;
            return;
        }

        _activeDictationProfile = _profileSelection.Current;
        _activeTranscriptionLanguageMode = _currentTranscriptionLanguageMode;
        _dictationStage = DictationFailureStage.Microphone;
        _audioRecorder.Start();
        _state = DictationState.Recording;
        DictationStateText.Text = _localizer["dictation.status.recording"];
        TargetStatusText.Text = DescribeTarget(_lockedTarget);
        LastResultText.Text = _localizer["dictation.recording.started"];
        ShowCapsule();
        _capsule?.ShowRecordingState();
        _ = PrepareModelForRecordingAsync();
    }

    private async Task CompleteDictationAsync()
    {
        _isBusy = true;
        _state = DictationState.Transcribing;
        DictationStateText.Text = _localizer["dictation.status.stoppingMic"];

        try
        {
            Stopwatch stopToTextTimer = Stopwatch.StartNew();
            _dictationStage = DictationFailureStage.Microphone;
            RecordedAudio audio = await _audioRecorder.StopAsync(_shutdown.Token);
            if (_isClosing || _shutdown.IsCancellationRequested)
            {
                return;
            }

            _capsule?.ShowProcessingState(_localizer["dictation.audio.preparing"]);
            if (audio.Duration < MinimumRecordingDuration)
            {
                DictationStateText.Text = _localizer["dictation.status.noSpeech"];
                LastResultText.Text = _localizer["dictation.tooShort"];
                return;
            }

            // ── Language resolution (Auto detection) ─────────────────
            _resolvedDictationLanguage = await ResolveTranscriptionLanguageAsync(
                audio, _activeTranscriptionLanguageMode, _shutdown.Token);

            _dictationStage = DictationFailureStage.Transcription;
            Progress<SpeechTranscriptionStage> progress = new(UpdateTranscriptionStage);
            string transcript = await _speechTranscriber.TranscribeAsync(
                audio.Samples,
                _resolvedDictationLanguage.ToWhisperCode(),
                progress,
                _shutdown.Token);
            if (_isClosing || _shutdown.IsCancellationRequested)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(transcript))
            {
                DictationStateText.Text = _localizer["dictation.status.noSpeech"];
                LastResultText.Text = _localizer["dictation.noSpeechRecognized"];
                return;
            }

            if (_isClosing || _shutdown.IsCancellationRequested)
            {
                return;
            }

            _shutdown.Token.ThrowIfCancellationRequested();
            IReadOnlyList<PersonalDictionaryEntry> dictionarySnapshot =
                _personalDictionary.CreateSnapshot();
            DictionaryProcessingResult dictionaryResult = _dictionaryProcessor.Apply(
                transcript,
                dictionarySnapshot);
            _shutdown.Token.ThrowIfCancellationRequested();

            RewriteProfile dictationProfile = _activeDictationProfile ?? RewriteProfiles.Default;
            _state = DictationState.Rewriting;
            DictationStateText.Text = _localizer["dictation.status.rewriting"];
            LastResultText.Text = dictionaryResult.ReplacementCount switch
            {
                0 => string.Format(_localizer["dictation.rewriting.profileApplied.zero"], dictationProfile.DisplayName),
                1 => string.Format(_localizer["dictation.rewriting.profileApplied.one"], dictationProfile.DisplayName),
                _ => string.Format(_localizer["dictation.rewriting.profileApplied.many"], dictionaryResult.ReplacementCount, dictationProfile.DisplayName)
            };
            _capsule?.ShowProcessingState(_localizer["dictation.rewriting.capsule"]);

            _dictationStage = DictationFailureStage.Rewriting;
            OrchestrationRewriteResult rewriteResult = await _rewriteOrchestrator.RewriteAsync(
                new OrchestrationRewriteRequest(
                    dictionaryResult.Text,
                    dictationProfile,
                    BuildRewriteContext(),
                    _resolvedDictationLanguage.ToWhisperCode()),
                _shutdown.Token);
            if (_isClosing || _shutdown.IsCancellationRequested)
            {
                return;
            }

            _dictationStage = DictationFailureStage.Insertion;
            bool delivered = InsertTranscript(
                rewriteResult,
                dictationProfile,
                dictionaryResult.ReplacementCount,
                audio.Duration,
                stopToTextTimer);

            if (delivered)
            {
                // Opt-in local history: records only when the user enabled it.
                // The page never throws back into the dictation flow.
                await HistoryPage.CaptureAsync(rewriteResult.Text, dictationProfile.Id);
            }
        }
        finally
        {
            _isBusy = false;
            _state = DictationState.Idle;
            _lockedTarget = null;
            _activeDictationProfile = null;
            ShowIdleCapsule();
        }
    }

    private bool InsertTranscript(
        OrchestrationRewriteResult rewriteResult,
        RewriteProfile dictationProfile,
        int dictionaryReplacementCount,
        TimeSpan audioDuration,
        Stopwatch stopToTextTimer)
    {
        TargetSnapshot? currentTarget = _targetDetector.CaptureActiveTarget();
        InsertionDecision decision = _textInsertionPolicy.Decide(_lockedTarget, currentTarget);
        string rewriteStatus = rewriteResult.Status switch
        {
            RewriteStatus.CloudApplied =>
                string.Format(_localizer["dictation.insert.profileVia"], dictationProfile.DisplayName, rewriteResult.ProviderUsed),
            RewriteStatus.LocalFallback =>
                string.Format(_localizer["dictation.insert.cloudUnavailable"], rewriteResult.FailureReason),
            _ => string.Format(_localizer["dictation.insert.profileLocal"], dictationProfile.DisplayName)
        };
        string dictionaryScope =
            _personalDictionary.StorageMode == DictionaryStorageMode.Persistent
                ? _localizer["dictation.scope.local"]
                : _localizer["dictation.scope.session"];
        string dictionaryStatus = dictionaryReplacementCount switch
        {
            0 => string.Empty,
            1 => " " + string.Format(_localizer["dictation.dictionary.one"], dictionaryScope),
            _ => " " + string.Format(_localizer["dictation.dictionary.many"], dictionaryReplacementCount, dictionaryScope)
        };

        bool delivered = false;

        switch (decision.Kind)
        {
            case InsertionDecisionKind.PasteIntoOriginalTarget:
                delivered = true;
                Clipboard.SetText(rewriteResult.Text);
                KeyboardInputSendResult sendResult = _keyboardInputSender.SendCtrlV();
                if (sendResult.Succeeded)
                {
                    string timing = FinishTiming(stopToTextTimer, audioDuration);
                    DictationStateText.Text = _localizer["dictation.status.inserted"];
                    LastResultText.Text =
                        _localizer["dictation.paste.success"] +
                        dictionaryStatus +
                        " " + rewriteStatus +
                        timing;
                }
                else
                {
                    string timing = FinishTiming(stopToTextTimer, audioDuration);
                    DictationStateText.Text = _localizer["dictation.status.clipboardFallback"];
                    LastResultText.Text =
                        string.Format(_localizer["dictation.paste.keyboardFailed"],
                            sendResult.SentInputCount, sendResult.RequestedInputCount, sendResult.ErrorCode) +
                        dictionaryStatus +
                        " " + rewriteStatus +
                        timing;
                }

                break;
            case InsertionDecisionKind.ClipboardFallbackTargetChanged:
                delivered = true;
                Clipboard.SetText(rewriteResult.Text);
                string changedTargetTiming = FinishTiming(stopToTextTimer, audioDuration);
                DictationStateText.Text = _localizer["dictation.status.clipboardFallback"];
                LastResultText.Text =
                    _localizer["dictation.paste.targetChanged"] +
                    dictionaryStatus +
                    " " + rewriteStatus +
                    changedTargetTiming;
                break;
            case InsertionDecisionKind.BlockedPasswordTarget:
                string passwordTiming = FinishTiming(stopToTextTimer, audioDuration);
                DictationStateText.Text = _localizer["dictation.status.blocked"];
                LastResultText.Text =
                    _localizer["dictation.paste.passwordBlocked"] + passwordTiming;
                break;
            case InsertionDecisionKind.BlockedUnverifiedTarget:
                string unverifiedTiming = FinishTiming(stopToTextTimer, audioDuration);
                DictationStateText.Text = _localizer["dictation.status.blocked"];
                LastResultText.Text =
                    _localizer["dictation.paste.unverified"] + unverifiedTiming;
                break;
            case InsertionDecisionKind.BlockedMissingTarget:
                string missingTiming = FinishTiming(stopToTextTimer, audioDuration);
                DictationStateText.Text = _localizer["dictation.status.blocked"];
                LastResultText.Text =
                    _localizer["dictation.paste.missing"] + missingTiming;
                break;
        }

        TargetStatusText.Text = currentTarget is null ? _localizer["dictation.paste.noTarget"] : DescribeTarget(currentTarget);
        return delivered;
    }

    private string FinishTiming(Stopwatch stopToTextTimer, TimeSpan audioDuration)
    {
        stopToTextTimer.Stop();
        return " " + string.Format(_localizer["dictation.timing"], stopToTextTimer.Elapsed.TotalSeconds, audioDuration.TotalSeconds);
    }

    private void OnOverviewNavigationClick(object sender, RoutedEventArgs e)
    {
        ShowDashboardPage(DashboardPage.Overview);
    }

    private void OnDictionaryNavigationClick(object sender, RoutedEventArgs e)
    {
        ShowDashboardPage(DashboardPage.Dictionary);
    }

    private void OnProfileChipClick(object sender, RoutedEventArgs e)
    {
        ShowDashboardPage(DashboardPage.Profiles);
    }

    private void OnSubscriptionNavigationClick(object sender, RoutedEventArgs e)
    {
        ShowDashboardPage(DashboardPage.Subscription);
    }

    private void OnUpgradeNavigationClick(object sender, RoutedEventArgs e)
    {
        ShowDashboardPage(DashboardPage.Subscription);
    }

    private void OnHistoryNavigationClick(object sender, RoutedEventArgs e)
    {
        ShowDashboardPage(DashboardPage.History);
    }

    private void OnSettingsNavigationClick(object sender, RoutedEventArgs e)
    {
        ShowDashboardPage(DashboardPage.Settings);
    }

    private void ShowDashboardPage(DashboardPage page)
    {
        OverviewPage.Visibility = page == DashboardPage.Overview ? Visibility.Visible : Visibility.Collapsed;
        DictionaryPage.Visibility = page == DashboardPage.Dictionary ? Visibility.Visible : Visibility.Collapsed;
        ProfilesPage.Visibility = page == DashboardPage.Profiles ? Visibility.Visible : Visibility.Collapsed;
        HistoryPage.Visibility = page == DashboardPage.History ? Visibility.Visible : Visibility.Collapsed;
        SettingsPage.Visibility = page == DashboardPage.Settings ? Visibility.Visible : Visibility.Collapsed;
        SubscriptionPage.Visibility = page == DashboardPage.Subscription ? Visibility.Visible : Visibility.Collapsed;

        SetNavActive(OverviewNavigationSurface, page == DashboardPage.Overview);
        SetNavActive(DictionaryNavigationSurface, page == DashboardPage.Dictionary);
        SetNavActive(HistoryNavigationSurface, page == DashboardPage.History);
        SetNavActive(SubscriptionNavigationSurface, page == DashboardPage.Subscription);
        SetNavActive(SettingsNavigationSurface, page == DashboardPage.Settings);
        SetNavActive(ProfileChipSurface, page == DashboardPage.Profiles);
    }

    private void SetNavActive(Border surface, bool active)
    {
        if (active)
        {
            surface.Background = (Brush)FindResource("Nyx.NavActiveBackgroundBrush");
        }
        else
        {
            // Clear the local value so the style default (transparent) and the
            // hover background trigger apply again.
            surface.ClearValue(Border.BackgroundProperty);
        }
    }

    private void UpdateUserProfileChip()
    {
        AuthenticatedUser? user = _authenticationState.User;
        if (user is null)
        {
            ProfileAvatarText.Text = "•";
            ProfileNameText.Text = _localizer["chip.localUser"];
            SubscriptionPage.SetAccount(_localizer["subscription.account.local"]);
            return;
        }

        string display = !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName!
            : user.Email ?? _localizer["dictation.user.fallback"];
        (string initials, string shortName) = FormatUserIdentity(display);
        ProfileAvatarText.Text = initials;
        ProfileNameText.Text = shortName;
        SubscriptionPage.SetAccount(
            user.Email is null
                ? _localizer["subscription.account.connectedNoEmail"]
                : $"{_localizer["subscription.account.connected"]}{user.Email}");
    }

    private (string Initials, string ShortName) FormatUserIdentity(string display)
    {
        string trimmed = display.Trim();
        int atIndex = trimmed.IndexOf('@');
        if (atIndex > 0 && !trimmed.Contains(' '))
        {
            trimmed = trimmed[..atIndex];
        }

        string[] parts = trimmed.Split(
            new[] { ' ', '.', '_', '-' },
            StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return ("•", _localizer["dictation.user.fallback"]);
        }

        if (parts.Length == 1)
        {
            string one = parts[0];
            string ini = one.Length >= 2 ? one[..2] : one;
            return (ini.ToUpperInvariant(), Capitalize(one));
        }

        string first = parts[0];
        string last = parts[^1];
        string initials = $"{first[0]}{last[0]}".ToUpperInvariant();
        string shortName = $"{Capitalize(first)} {char.ToUpperInvariant(last[0])}";
        return (initials, shortName);
    }

    private static string Capitalize(string value)
    {
        return value.Length == 0
            ? value
            : char.ToUpperInvariant(value[0]) + value[1..];
    }

    private void OnHistoryEntryCountChanged(object? sender, int count)
    {
        _historyEntryCount = count;
        UpdateHistoryNavStatus();
    }

    private void OnHistoryEnabledChanged(object? sender, bool enabled)
    {
        UpdateHistoryNavStatus();
    }

    private void UpdateHistoryNavStatus()
    {
        HistoryNavigationStatusText.Text = HistoryPage.HistoryEnabled
            ? _historyEntryCount.ToString()
            : _localizer["dictation.history.off"];
        SettingsPage.SetHistorySummary(HistoryPage.HistoryEnabled, _historyEntryCount);
    }

    private void OnProfileSelectionChanged(object? sender, RewriteProfile profile)
    {
        UpdateProfilePresentation(profile);
        PersistPreferredProfile(profile.Id);
        SettingsPage.SetProfiles(RewriteProfiles.All, profile);
    }

    private void OnSettingsDefaultProfileChangeRequested(object? sender, string profileId)
    {
        ProfileSelectionResult result = _profileSelection.TrySelect(profileId);
        if (!result.Succeeded)
        {
            return;
        }

        UpdateProfilePresentation(result.Selected);
        PersistPreferredProfile(result.Selected.Id);
        ProfilesPage.Initialize(_profileSelection);
        SettingsPage.SetProfiles(RewriteProfiles.All, _profileSelection.Current);
    }

    private void OnSettingsOpenHistoryRequested(object? sender, EventArgs e)
    {
        ShowDashboardPage(DashboardPage.History);
    }

    /// <summary>
    /// Restores the user's persisted preferred rewrite profile on launch. Absent
    /// or unknown values leave the canonical default in place.
    /// </summary>
    private async Task RestorePreferencesAsync()
    {
        AppPreferences preferences =
            await _appSettingsStore.InitializeAndLoadAsync(_shutdown.Token);

        ApplyLanguage(preferences.Language);
        SettingsPage.SetLanguageSelection(preferences.Language);

        _currentTranscriptionLanguageMode =
            TranscriptionLanguageCatalog.ParseModeOrDefault(
                preferences.TranscriptionLanguageId);
        SettingsPage.SetTranscriptionLanguageSelection(
            TranscriptionLanguageCatalog.PersistMode(
                _currentTranscriptionLanguageMode));

        if (preferences.PreferredProfileId is null)
        {
            return;
        }

        ProfileSelectionResult result =
            _profileSelection.TrySelect(preferences.PreferredProfileId);
        if (!result.Succeeded)
        {
            return;
        }

        ProfilesPage.Initialize(_profileSelection);
        SettingsPage.SetProfiles(RewriteProfiles.All, _profileSelection.Current);
        UpdateProfilePresentation(_profileSelection.Current);
    }

    /// <summary>
    /// Applies the interface language to the navigation chrome. First slice:
    /// navigation labels; other page text is localised incrementally.
    /// </summary>
    private void ApplyLanguage(string language)
    {
        // All localized text is bound to the Localizer; changing its language
        // refreshes every bound string across every page at once.
        _localizer.Language = language == "fr" ? "fr" : "en";
        UpdateUserProfileChip();
        RefreshDashboardStatusPresentation();
    }

    private async void OnLanguageChangeRequested(object? sender, string language)
    {
        string normalized = AppSettingsLimits.NormalizeLanguage(language);
        ApplyLanguage(normalized);
        SettingsPage.SetLanguageSelection(normalized);

        try
        {
            await _appSettingsStore.SetLanguageAsync(normalized, _shutdown.Token);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Language persistence failed: {ex.GetBaseException().Message}");
        }
    }

    private async void OnTranscriptionLanguageChangeRequested(
        object? sender, string languageId)
    {
        string normalized =
            AppSettingsLimits.NormalizeTranscriptionLanguage(languageId);
        _currentTranscriptionLanguageMode =
            TranscriptionLanguageCatalog.ParseModeOrDefault(normalized);
        SettingsPage.SetTranscriptionLanguageSelection(normalized);

        try
        {
            await _appSettingsStore.SetTranscriptionLanguageAsync(
                normalized, _shutdown.Token);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"Transcription language persistence failed: {ex.GetBaseException().Message}");
        }
    }

    /// <summary>
    /// Best-effort local persistence of the preferred profile. A failure here
    /// never disrupts the dictation experience.
    /// </summary>
    private async void PersistPreferredProfile(string profileId)
    {
        try
        {
            await _appSettingsStore.SetPreferredProfileAsync(profileId, _shutdown.Token);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Preferred-profile persistence failed: {ex.GetBaseException().Message}");
        }
    }

    /// <summary>
    /// Builds the per-dictation gate. Cloud is requested only when the user is authenticated
    /// AND has enabled Cloud rewriting AND has granted consent; otherwise the provider stays
    /// Local, so being merely authenticated never sends text to the Cloud.
    /// </summary>
    private void OnCloudStateChanged(object? sender, EventArgs e)
    {
        UpdateProfilePresentation(_profileSelection.Current);
    }

    private void OnAuthenticationStateChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => OnAuthenticationStateChanged(sender, e));
            return;
        }

        UpdateProfilePresentation(_profileSelection.Current);
        UpdateUserProfileChip();
    }

    /// <summary>
    /// Resolved per cloud call. Until an authentication phase supplies a backend address and
    /// access token, BaseAddress stays null, so the transport fails closed and the exact local
    /// text is used. This is a deliberate deferred item, not a silent gap.
    /// </summary>
    private Fluent.Cloud.CloudBackendOptions BuildCloudBackendOptions()
    {
        return new Fluent.Cloud.CloudBackendOptions
        {
            BaseAddress = _cloudBackendConfiguration?.Origin,
            AccessToken = _authenticationState.AccessToken
        };
    }

    private RewriteContext BuildRewriteContext()
    {
        bool cloudRequested =
            _authenticationState.IsAuthenticated
            && _cloudRewriteSettings.CloudRewriteEnabled
            && _cloudRewriteSettings.CloudConsentGranted
            && BuildCloudBackendOptions().BaseAddress is not null;

        return new RewriteContext(
            _authenticationState.IsAuthenticated,
            _cloudRewriteSettings.CloudRewriteEnabled,
            _cloudRewriteSettings.CloudConsentGranted,
            cloudRequested ? _cloudRewriteSettings.SelectedProvider : RewriteProviderId.Local);
    }

    private void UpdateProfilePresentation(RewriteProfile profile)
    {
        RewriteContext context = BuildRewriteContext();
        string modeLabel = context.IsCloudEligible
            ? string.Format(_localizer["dictation.profile.cloudWith"], ProviderDisplayName(context.Provider))
            : _localizer["dictation.profile.local"];
        SettingsPage.SetSessionInfo(profile.DisplayName, modeLabel, _localizer["dictation.engine.label"]);
        ProfileSummaryText.Text = _localizer["dictation.profile.prefix"] + profile.DisplayName;
        RefreshDashboardStatusPresentation();
    }

    private static string ProviderDisplayName(RewriteProviderId provider) => provider switch
    {
        RewriteProviderId.DeepSeek => "DeepSeek",
        RewriteProviderId.Gemini => "Gemini",
        _ => "Local"
    };

    private void OnDictionaryEntryCountChanged(object? sender, int count)
    {
        UpdateDictionaryPresentation(count);
    }

    private void OnDictionaryStorageModeChanged(
        object? sender,
        DictionaryStorageMode storageMode)
    {
        UpdateDictionaryPresentation(_personalDictionary.Count);
    }

    private void UpdateDictionaryPresentation(int count)
    {
        switch (_personalDictionary.StorageMode)
        {
            case DictionaryStorageMode.Persistent:
                DictionaryNavigationStatusText.Text = _localizer["dictation.nav.local"];
                break;
            case DictionaryStorageMode.SessionOnlyFallback:
                DictionaryNavigationStatusText.Text = _localizer["dictation.nav.session"];
                break;
            default:
                DictionaryNavigationStatusText.Text = _localizer["dictation.nav.loading"];
                break;
        }

        RefreshDashboardStatusPresentation();
    }

    /// <summary>
    /// Updates the Overview from existing in-process state only. This presentation neither
    /// persists data nor drives authentication, Cloud consent, provider selection, or requests.
    /// </summary>
    private void RefreshDashboardStatusPresentation()
    {
        DashboardStatusPresentation presentation = DashboardStatusPresenter.Create(
            new DashboardStatusInput(
                _profileSelection.Current.DisplayName,
                _personalDictionary.Count,
                DictionaryPage.StorageMode,
                _authenticationState.Status,
                _authenticationState.IsAuthenticated,
                _cloudBackendConfiguration is not null,
                _cloudRewriteSettings.CloudRewriteEnabled,
                _cloudRewriteSettings.CloudConsentGranted,
                _cloudRewriteSettings.SelectedProvider),
            _localizer.Language);

        ProfileSummaryText.Text = presentation.ProfileSummary;
        DictionarySummaryText.Text = presentation.DictionarySummary;
        AuthenticationSummaryText.Text = presentation.AuthenticationSummary;
        CloudSummaryText.Text = presentation.CloudSummary;
    }

    private void UpdateTranscriptionStage(SpeechTranscriptionStage stage)
    {
        if (_state != DictationState.Transcribing || _isClosing)
        {
            return;
        }

        (string state, string capsuleText) = stage switch
        {
            SpeechTranscriptionStage.PreparingModel => (_localizer["dictation.status.preparing"], _localizer["dictation.status.preparing"] + "…"),
            SpeechTranscriptionStage.DownloadingModel => (_localizer["dictation.status.downloading"], _localizer["dictation.model.downloading"]),
            SpeechTranscriptionStage.LoadingModel => (_localizer["dictation.status.loading"], _localizer["dictation.status.loading"] + "…"),
            SpeechTranscriptionStage.WarmingModel => (_localizer["dictation.status.optimizing"], _localizer["dictation.status.optimizing"] + "…"),
            SpeechTranscriptionStage.Transcribing => (_localizer["dictation.status.transcribing"], _localizer["dictation.status.transcribing"] + "…"),
            _ => (_localizer["dictation.status.transcribing"], _localizer["dictation.status.transcribing"] + "…")
        };

        DictationStateText.Text = state;
        _capsule?.ShowProcessingState(capsuleText);
        LastResultText.Text = stage == SpeechTranscriptionStage.DownloadingModel
            ? _localizer["dictation.model.firstUse"]
            : _localizer["dictation.model.processing"];
    }

    private async Task PrepareModelForRecordingAsync()
    {
        try
        {
            Progress<SpeechTranscriptionStage> progress = new(UpdateRecordingPreparationStage);
            await _speechTranscriber.PrepareAsync(progress, _shutdown.Token);

            if (!_isClosing && _state == DictationState.Recording)
            {
                DictationStateText.Text = _localizer["dictation.status.recording"];
                LastResultText.Text = _localizer["dictation.model.ready"];
                _capsule?.ShowRecordingState();
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (!_isClosing && _state == DictationState.Recording)
            {
                DictationStateText.Text = _localizer["dictation.status.recording"];
                LastResultText.Text =
                    string.Format(_localizer["dictation.model.retryAfterStop"], ex.GetBaseException().Message);
                _capsule?.ShowRecordingState();
            }
        }
    }

    private void UpdateRecordingPreparationStage(SpeechTranscriptionStage stage)
    {
        if (_state != DictationState.Recording || _isClosing)
        {
            return;
        }

        DictationStateText.Text = _localizer["dictation.status.recording"];
        LastResultText.Text = stage == SpeechTranscriptionStage.DownloadingModel
            ? _localizer["dictation.prep.firstUseDownload"]
            : _localizer["dictation.prep.background"];
        _capsule?.ShowRecordingState();
    }

    private void ResetToIdle()
    {
        _isBusy = false;
        _state = DictationState.Idle;
        _lockedTarget = null;
        _activeDictationProfile = null;
        ShowIdleCapsule();
    }

    private void ShowCapsule()
    {
        if (_capsule is null)
        {
            _capsule = new RecordingCapsuleWindow { Owner = this };
            _capsule.Left = (SystemParameters.WorkArea.Width - _capsule.Width) / 2
                + SystemParameters.WorkArea.Left;
            _capsule.Top = SystemParameters.WorkArea.Bottom - _capsule.Height - 24;
        }

        if (!_capsule.IsVisible)
        {
            _capsule.Show();
        }
    }

    private void ShowIdleCapsule()
    {
        if (_isClosing || _hotKey is null)
        {
            return;
        }

        ShowCapsule();
        _capsule?.ShowIdleState();
    }

    private string DescribeTarget(TargetSnapshot target)
    {
        string element = string.IsNullOrWhiteSpace(target.FocusedElementControlType)
            ? _localizer["dictation.target.unknown"]
            : target.FocusedElementControlType;

        return $"{target.WindowTitle} [{target.WindowClassName}], pid {target.ProcessId}, {element}";
    }

    private enum DictationState
    {
        Idle,
        Recording,
        Transcribing,
        Rewriting
    }

    private enum DashboardPage
    {
        Overview,
        Dictionary,
        Profiles,
        History,
        Settings,
        Subscription
    }

    private async Task<TranscriptionLanguage> ResolveTranscriptionLanguageAsync(
        RecordedAudio audio,
        TranscriptionLanguageMode mode,
        CancellationToken cancellationToken)
    {
        // Manual modes bypass detection entirely.
        if (mode == TranscriptionLanguageMode.French)
        {
            return TranscriptionLanguage.French;
        }

        if (mode == TranscriptionLanguageMode.English)
        {
            return TranscriptionLanguage.English;
        }

        // Auto mode: run detection.
        try
        {
            float[] samples = audio.Duration.TotalSeconds > 5
                ? audio.Samples[..Math.Min(audio.Samples.Length, 16000 * 5)]
                : audio.Samples;

            (string? detectedCode, float probability) =
                await _speechTranscriber.DetectLanguageAsync(
                    samples, cancellationToken);

            TranscriptionLanguage? detected =
                TranscriptionLanguageCatalog.ParseConcreteLanguageOrNull(detectedCode);

            if (detected is not null && probability >= DetectionThresholds.MinimumConfidence)
            {
                _lastDetectedLanguage = detected.Value;
                return detected.Value;
            }

            // Fallback: use last detected language or French.
            if (_lastDetectedLanguage is not null)
            {
                return _lastDetectedLanguage.Value;
            }

            return TranscriptionLanguage.French;
        }
        catch (OperationCanceledException)
        {
            return TranscriptionLanguage.French;
        }
        catch
        {
            return TranscriptionLanguage.French;
        }
    }

    private static void DarkenTitleBar(nint handle)
    {
        // DWMWA_USE_IMMERSIVE_DARK_MODE = 20 (Windows 10 20H1+)
        // DWMWA_CAPTION_COLOR = 35 (Windows 11)
        int useDarkMode = 1;
        DwmSetWindowAttribute(handle, 20, ref useDarkMode, sizeof(int));

        // Dark grey caption: #18181C → 0x001C1818 (ABGR)
        int captionColor = 0x001C1818;
        DwmSetWindowAttribute(handle, 35, ref captionColor, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        nint hwnd, int attr, ref int attrValue, int attrSize);
}
