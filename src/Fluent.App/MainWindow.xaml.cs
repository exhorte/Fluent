using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Fluent.App.Auth;
using Fluent.App.Cloud;
using Fluent.App.Dashboard;
using Fluent.App.Phase01;
using Fluent.Audio.Capture;
using Fluent.App.Localization;
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
            HotkeyStatusText.Text = "Actif";
            DictationStateText.Text = "Prêt";
            ShowIdleCapsule();
        }
        catch (Exception ex)
        {
            HotkeyStatusText.Text = "Indisponible";
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
            LastResultText.Text = "Fluent traite déjà la dictée en cours.";
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
            DictationStateText.Text = "Échec";
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
            DictationStateText.Text = "Blocked";
            LastResultText.Text = "No active target could be captured.";
            return;
        }

        if (_lockedTarget.IsPasswordTarget)
        {
            DictationStateText.Text = "Blocked";
            TargetStatusText.Text = DescribeTarget(_lockedTarget);
            LastResultText.Text = "Password target blocked. Nothing was recorded, pasted, or copied.";
            _lockedTarget = null;
            return;
        }

        if (!_lockedTarget.IsUsable)
        {
            DictationStateText.Text = "Blocked";
            TargetStatusText.Text = DescribeTarget(_lockedTarget);
            LastResultText.Text =
                "Target safety or focused-field identity could not be verified. Nothing was recorded, pasted, or copied.";
            _lockedTarget = null;
            return;
        }

        _activeDictationProfile = _profileSelection.Current;
        _dictationStage = DictationFailureStage.Microphone;
        _audioRecorder.Start();
        _state = DictationState.Recording;
        DictationStateText.Text = "Recording";
        TargetStatusText.Text = DescribeTarget(_lockedTarget);
        LastResultText.Text = "Recording in memory. Press Ctrl+Space again to transcribe and insert.";
        ShowCapsule();
        _capsule?.ShowRecordingState();
        _ = PrepareModelForRecordingAsync();
    }

    private async Task CompleteDictationAsync()
    {
        _isBusy = true;
        _state = DictationState.Transcribing;
        DictationStateText.Text = "Stopping microphone";

        try
        {
            Stopwatch stopToTextTimer = Stopwatch.StartNew();
            _dictationStage = DictationFailureStage.Microphone;
            RecordedAudio audio = await _audioRecorder.StopAsync(_shutdown.Token);
            if (_isClosing || _shutdown.IsCancellationRequested)
            {
                return;
            }

            _capsule?.ShowProcessingState("Préparation audio…");
            if (audio.Duration < MinimumRecordingDuration)
            {
                DictationStateText.Text = "No speech";
                LastResultText.Text = "Recording was too short. Nothing was pasted or copied.";
                return;
            }

            _dictationStage = DictationFailureStage.Transcription;
            Progress<SpeechTranscriptionStage> progress = new(UpdateTranscriptionStage);
            string transcript = await _speechTranscriber.TranscribeFrenchAsync(
                audio.Samples,
                progress,
                _shutdown.Token);
            if (_isClosing || _shutdown.IsCancellationRequested)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(transcript))
            {
                DictationStateText.Text = "No speech";
                LastResultText.Text = "No speech was recognized. Nothing was pasted or copied.";
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
            DictationStateText.Text = "Réécriture";
            LastResultText.Text = dictionaryResult.ReplacementCount switch
            {
                0 => $"Application locale du profil {dictationProfile.DisplayName}.",
                1 => $"1 correction du dictionnaire appliquée avant le profil {dictationProfile.DisplayName}.",
                _ => $"{dictionaryResult.ReplacementCount} corrections du dictionnaire appliquées avant le profil {dictationProfile.DisplayName}."
            };
            _capsule?.ShowProcessingState("Réécriture…");

            _dictationStage = DictationFailureStage.Rewriting;
            OrchestrationRewriteResult rewriteResult = await _rewriteOrchestrator.RewriteAsync(
                new OrchestrationRewriteRequest(
                    dictionaryResult.Text,
                    dictationProfile,
                    BuildRewriteContext()),
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
                $" Profil {dictationProfile.DisplayName} appliqué via {rewriteResult.ProviderUsed}.",
            RewriteStatus.LocalFallback =>
                $" Service Cloud indisponible ({rewriteResult.FailureReason}) : texte local exact conservé.",
            _ => $" Profil {dictationProfile.DisplayName} appliqué localement."
        };
        string dictionaryScope =
            _personalDictionary.StorageMode == DictionaryStorageMode.Persistent
                ? "local dictionary"
                : "session-only dictionary";
        string dictionaryStatus = dictionaryReplacementCount switch
        {
            0 => string.Empty,
            1 => $" 1 {dictionaryScope} correction applied.",
            _ => $" {dictionaryReplacementCount} {dictionaryScope} corrections applied."
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
                    DictationStateText.Text = "Inserted";
                    LastResultText.Text =
                        "French speech transcribed locally and inserted into the locked target." +
                        dictionaryStatus +
                        rewriteStatus +
                        timing;
                }
                else
                {
                    string timing = FinishTiming(stopToTextTimer, audioDuration);
                    DictationStateText.Text = "Clipboard fallback";
                    LastResultText.Text =
                        $"Paste shortcut could not be sent " +
                        $"({sendResult.SentInputCount}/{sendResult.RequestedInputCount} inputs, " +
                        $"Windows error {sendResult.ErrorCode}). The transcript remains on the clipboard." +
                        dictionaryStatus +
                        rewriteStatus +
                        timing;
                }

                break;
            case InsertionDecisionKind.ClipboardFallbackTargetChanged:
                delivered = true;
                Clipboard.SetText(rewriteResult.Text);
                string changedTargetTiming = FinishTiming(stopToTextTimer, audioDuration);
                DictationStateText.Text = "Clipboard fallback";
                LastResultText.Text =
                    "Target changed during dictation. Transcript copied to clipboard, not pasted." +
                    dictionaryStatus +
                    rewriteStatus +
                    changedTargetTiming;
                break;
            case InsertionDecisionKind.BlockedPasswordTarget:
                string passwordTiming = FinishTiming(stopToTextTimer, audioDuration);
                DictationStateText.Text = "Blocked";
                LastResultText.Text =
                    "Password target blocked. Transcript was not pasted or copied." + passwordTiming;
                break;
            case InsertionDecisionKind.BlockedUnverifiedTarget:
                string unverifiedTiming = FinishTiming(stopToTextTimer, audioDuration);
                DictationStateText.Text = "Blocked";
                LastResultText.Text =
                    "Target safety could not be verified. Transcript was not pasted or copied." + unverifiedTiming;
                break;
            case InsertionDecisionKind.BlockedMissingTarget:
                string missingTiming = FinishTiming(stopToTextTimer, audioDuration);
                DictationStateText.Text = "Blocked";
                LastResultText.Text =
                    "Target missing. Transcript was not pasted or copied." + missingTiming;
                break;
        }

        TargetStatusText.Text = currentTarget is null ? "No current target" : DescribeTarget(currentTarget);
        return delivered;
    }

    private static string FinishTiming(Stopwatch stopToTextTimer, TimeSpan audioDuration)
    {
        stopToTextTimer.Stop();
        return $" Stop-to-text: {stopToTextTimer.Elapsed.TotalSeconds:F1}s for {audioDuration.TotalSeconds:F1}s of audio.";
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
            surface.BorderBrush = (Brush)FindResource("Nyx.NavActiveOutlineBrush");
        }
        else
        {
            // Clear the local value so the style default (transparent) and the
            // hover outline trigger apply again.
            surface.ClearValue(Border.BorderBrushProperty);
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
            : user.Email ?? "Utilisateur";
        (string initials, string shortName) = FormatUserIdentity(display);
        ProfileAvatarText.Text = initials;
        ProfileNameText.Text = shortName;
        SubscriptionPage.SetAccount(
            user.Email is null
                ? _localizer["subscription.account.connectedNoEmail"]
                : $"{_localizer["subscription.account.connected"]}{user.Email}");
    }

    private static (string Initials, string ShortName) FormatUserIdentity(string display)
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
            return ("•", "Utilisateur");
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
            : "OFF";
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
            ? $"Cloud · {ProviderDisplayName(context.Provider)}"
            : "Local";
        SettingsPage.SetSessionInfo(profile.DisplayName, modeLabel, "Base Q8 · CPU");
        ProfileSummaryText.Text = $"Profil · {profile.DisplayName}";
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
                DictionaryNavigationStatusText.Text = "LOCAL";
                break;
            case DictionaryStorageMode.SessionOnlyFallback:
                DictionaryNavigationStatusText.Text = "SESSION";
                break;
            default:
                DictionaryNavigationStatusText.Text = "CHARGEMENT";
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
            SpeechTranscriptionStage.PreparingModel => ("Préparation", "Préparation…"),
            SpeechTranscriptionStage.DownloadingModel => ("Téléchargement", "Modèle ~80 Mo…"),
            SpeechTranscriptionStage.LoadingModel => ("Chargement", "Chargement…"),
            SpeechTranscriptionStage.WarmingModel => ("Optimisation", "Optimisation…"),
            SpeechTranscriptionStage.Transcribing => ("Transcription", "Transcription…"),
            _ => ("Transcription", "Transcription…")
        };

        DictationStateText.Text = state;
        _capsule?.ShowProcessingState(capsuleText);
        LastResultText.Text = stage == SpeechTranscriptionStage.DownloadingModel
            ? "First use only: downloading the multilingual Whisper model. Audio remains local."
            : "Processing locally. Audio is held in memory only.";
    }

    private async Task PrepareModelForRecordingAsync()
    {
        try
        {
            Progress<SpeechTranscriptionStage> progress = new(UpdateRecordingPreparationStage);
            await _speechTranscriber.PrepareAsync(progress, _shutdown.Token);

            if (!_isClosing && _state == DictationState.Recording)
            {
                DictationStateText.Text = "Recording";
                LastResultText.Text =
                    "Recording in memory. The local transcription model is ready; press Ctrl+Space to finish.";
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
                DictationStateText.Text = "Recording";
                LastResultText.Text =
                    $"Recording continues. Model preparation will retry after stop: {ex.GetBaseException().Message}";
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

        DictationStateText.Text = "Enregistrement";
        LastResultText.Text = stage == SpeechTranscriptionStage.DownloadingModel
            ? "Premier usage : téléchargement du modèle local pendant l'enregistrement. L'audio reste local."
            : "Enregistrement en cours. Préparation locale du moteur en arrière-plan.";
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
            _capsule.Left = SystemParameters.WorkArea.Right - _capsule.Width - 24;
            _capsule.Top = SystemParameters.WorkArea.Top + 24;
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

    private static string DescribeTarget(TargetSnapshot target)
    {
        string element = string.IsNullOrWhiteSpace(target.FocusedElementControlType)
            ? "unknown focused element"
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
}
