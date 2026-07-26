using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fluent.App.Auth;
using Fluent.App.Cloud;
using Fluent.App.Localization;
using Fluent.Rewrite.Profiles;
using Fluent.Rewrite.Providers;

namespace Fluent.App.Views;

public partial class ProfilesPage : UserControl
{
    private ProfileSelection? _selection;
    private CloudRewriteSettings? _cloudSettings;
    private IAuthenticationState? _authenticationState;
    private bool _cloudBackendAvailable;
    private string _cloudBackendUnavailableReason;
    private readonly Localizer _localizer =
        (Localizer)Application.Current.Resources["Loc"];

    public ProfilesPage()
    {
        InitializeComponent();
        _cloudBackendUnavailableReason = _localizer["profiles.cloud.backendNotConfigured"];
    }

    public event EventHandler<RewriteProfile>? SelectionChanged;

    public event EventHandler? CloudStateChanged;

    /// <summary>
    /// Wires the session-only Cloud state. Nothing is persisted and no secret is held here.
    /// </summary>
    public void InitializeCloud(
        CloudRewriteSettings cloudSettings,
        IAuthenticationState authenticationState,
        bool cloudBackendAvailable,
        string cloudBackendUnavailableReason)
    {
        ArgumentNullException.ThrowIfNull(cloudSettings);
        ArgumentNullException.ThrowIfNull(authenticationState);

        if (_authenticationState is not null)
        {
            _authenticationState.Changed -= OnAuthenticationStateChanged;
        }

        _cloudSettings = cloudSettings;
        _authenticationState = authenticationState;
        _cloudBackendAvailable = cloudBackendAvailable;
        _cloudBackendUnavailableReason = string.IsNullOrWhiteSpace(cloudBackendUnavailableReason)
            ? _localizer["profiles.cloud.backendNotConfigured"]
            : cloudBackendUnavailableReason;
        _authenticationState.Changed += OnAuthenticationStateChanged;
        RefreshAuthenticationCard();
        RefreshCloudCard();
    }

    private async void AuthenticationActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_authenticationState is null)
        {
            return;
        }

        if (_authenticationState.IsOperationInProgress)
        {
            _authenticationState.CancelSignIn();
            return;
        }

        if (_authenticationState.IsAuthenticated)
        {
            await _authenticationState.SignOutAsync();
            return;
        }

        await _authenticationState.SignInWithGoogleAsync();
    }

    private void AuthenticationCancelButton_Click(object sender, RoutedEventArgs e)
    {
        _authenticationState?.CancelSignIn();
    }

    private void OnAuthenticationStateChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => OnAuthenticationStateChanged(sender, e));
            return;
        }

        RefreshAuthenticationCard();
        RefreshCloudCard();
        CloudStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CloudToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_cloudSettings is null)
        {
            return;
        }

        if (_authenticationState is null || !_authenticationState.IsAuthenticated)
        {
            RefreshCloudCard();
            return;
        }

        if (_cloudSettings.CloudRewriteEnabled)
        {
            _cloudSettings.Disable();
            RefreshCloudCard();
            CloudStateChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (!_cloudSettings.CloudConsentGranted && !RequestConsent())
        {
            return;
        }

        _cloudSettings.TryEnable();
        RefreshCloudCard();
        CloudStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ProviderSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_cloudSettings is null
            || _authenticationState is null
            || !_authenticationState.IsAuthenticated
            || !_cloudBackendAvailable
            || sender is not Button { Tag: string providerId })
        {
            return;
        }

        RewriteProviderId provider = providerId switch
        {
            "gemini" => RewriteProviderId.Gemini,
            "deepseek" => RewriteProviderId.DeepSeek,
            _ => RewriteProviderId.Local
        };

        if (_cloudSettings.TrySelectProvider(provider))
        {
            RefreshCloudCard();
            CloudStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool RequestConsent()
    {
        string message = _localizer["profiles.cloud.consent.message"];
        string caption = _localizer["profiles.cloud.consent.caption"];

        MessageBoxResult answer = MessageBox.Show(
            message,
            caption,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            return false;
        }

        _cloudSettings!.GrantConsent();
        return true;
    }

    private void RefreshCloudCard()
    {
        if (_cloudSettings is null || _authenticationState is null)
        {
            return;
        }

        bool authenticated = _authenticationState.IsAuthenticated;
        bool enabled = _cloudSettings.CloudRewriteEnabled;
        RewriteProviderId provider = _cloudSettings.SelectedProvider;
        string providerName = ProviderDisplayName(provider);
        bool canSelectProvider = authenticated && _cloudBackendAvailable;

        CloudToggleButton.Content = enabled
            ? _localizer["profiles.cloud.toggle.disable"]
            : _localizer["profiles.cloud.toggle.enable"];
        CloudToggleButton.IsEnabled = CanActivateCloud(authenticated, _cloudBackendAvailable);
        GeminiProviderButton.Content = provider == RewriteProviderId.Gemini
            ? string.Format(_localizer["profiles.cloud.provider.active"], "Gemini")
            : "Gemini";
        DeepSeekProviderButton.Content = provider == RewriteProviderId.DeepSeek
            ? string.Format(_localizer["profiles.cloud.provider.active"], "DeepSeek")
            : "DeepSeek V4 Pro";
        GeminiProviderButton.IsEnabled = canSelectProvider && provider != RewriteProviderId.Gemini;
        DeepSeekProviderButton.IsEnabled = canSelectProvider && provider != RewriteProviderId.DeepSeek;

        if (!authenticated)
        {
            CloudBadgeText.Text = _localizer["profiles.cloud.badge.local"];
            CloudStatusText.Text = _authenticationState.Status == AuthenticationStatus.Unconfigured
                ? _localizer["profiles.cloud.status.unauthenticated.unconfigured"]
                : _localizer["profiles.cloud.status.unauthenticated.expired"];
            CloudConsentText.Text = _localizer["profiles.cloud.consent.unauthenticated"];
            return;
        }

        if (!_cloudBackendAvailable)
        {
            CloudBadgeText.Text = _localizer["profiles.cloud.badge.unavailable"];
            CloudStatusText.Text = enabled
                ? string.Format(_localizer["profiles.cloud.status.backendUnavailable.enabled"], providerName, _cloudBackendUnavailableReason)
                : string.Format(_localizer["profiles.cloud.status.backendUnavailable.disabled"], _cloudBackendUnavailableReason);
            CloudConsentText.Text = _cloudSettings.CloudConsentGranted
                ? _localizer["profiles.cloud.consent.backendUnavailable.granted"]
                : _localizer["profiles.cloud.consent.backendUnavailable.notGranted"];
            return;
        }

        CloudBadgeText.Text = enabled
            ? string.Format(_localizer["profiles.cloud.badge.active"], providerName.ToUpperInvariant())
            : _localizer["profiles.cloud.badge.local"];
        CloudStatusText.Text = enabled
            ? string.Format(_localizer["profiles.cloud.status.enabled"], providerName)
            : _localizer["profiles.cloud.status.disabled"];
        CloudConsentText.Text = _cloudSettings.CloudConsentGranted
            ? _localizer["profiles.cloud.consent.granted"]
            : _localizer["profiles.cloud.consent.notGranted"];
    }

    private static string ProviderDisplayName(RewriteProviderId provider) => provider switch
    {
        RewriteProviderId.DeepSeek => "DeepSeek",
        _ => "Gemini"
    };

    /// <summary>
    /// Cloud activation is a presentation gate in addition to the per-dictation domain gate.
    /// A configured backend origin is required before consent can be requested or Cloud can be
    /// enabled for the current session.
    /// </summary>
    public static bool CanActivateCloud(bool isAuthenticated, bool hasConfiguredBackendOrigin)
    {
        return isAuthenticated && hasConfiguredBackendOrigin;
    }

    private void RefreshAuthenticationCard()
    {
        if (_authenticationState is null)
        {
            return;
        }

        AuthenticationStatus status = _authenticationState.Status;
        bool authenticated = _authenticationState.IsAuthenticated;
        bool signingIn = _authenticationState.IsOperationInProgress;

        AuthenticationBadgeText.Text = status switch
        {
            AuthenticationStatus.Authenticated => _localizer["profiles.auth.badge.connected"],
            AuthenticationStatus.SigningIn => _localizer["profiles.auth.badge.signingIn"],
            AuthenticationStatus.Offline => _localizer["profiles.auth.badge.offline"],
            AuthenticationStatus.Expired => _localizer["profiles.auth.badge.expired"],
            AuthenticationStatus.Failed => _localizer["profiles.auth.badge.failed"],
            AuthenticationStatus.Unconfigured => _localizer["profiles.auth.badge.unconfigured"],
            _ => _localizer["profiles.auth.badge.local"]
        };
        AuthenticationStatusText.Text = _authenticationState.StatusMessage;
        AuthenticationActionButton.Content = authenticated
            ? _localizer["profiles.auth.signOut"]
            : signingIn
                ? _localizer["profiles.auth.signingIn"]
                : _localizer["profiles.auth.signInGoogle"];
        AuthenticationActionButton.IsEnabled = !signingIn && status != AuthenticationStatus.Unconfigured;
        AuthenticationCancelButton.Visibility = signingIn ? Visibility.Visible : Visibility.Collapsed;

        AuthenticatedUser? user = _authenticationState.User;
        AuthenticatedUserPanel.Visibility = user is null ? Visibility.Collapsed : Visibility.Visible;
        AuthenticatedUserNameText.Text = user?.DisplayName ?? user?.Email ?? _localizer["profiles.auth.connected"];
        AuthenticatedUserEmailText.Text = user?.Email ?? string.Empty;
    }

    public void Initialize(ProfileSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        _selection = selection;
        RefreshCards();
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selection is null || sender is not Button { Tag: string profileId })
        {
            return;
        }

        ProfileSelectionResult result = _selection.TrySelect(profileId);
        SetSelectionStatus(result.Message, result.Succeeded);
        RefreshCards();

        if (result.Succeeded)
        {
            SelectionChanged?.Invoke(this, result.Selected);
        }
    }

    private void RefreshCards()
    {
        RewriteProfile? current = _selection?.Current;
        List<ProfileCardModel> cards = new(RewriteProfiles.All.Count);

        foreach (RewriteProfile profile in RewriteProfiles.All)
        {
            bool isCurrent = current is not null && ReferenceEquals(profile, current);
            cards.Add(CreateCard(profile, isCurrent));
        }

        ProfilesItemsControl.ItemsSource = cards;
    }

    private ProfileCardModel CreateCard(RewriteProfile profile, bool isCurrent)
    {
        Brush cyan = (Brush)FindResource("Nyx.CyanBrush");
        Brush muted = (Brush)FindResource("Nyx.MutedTextBrush");
        Brush secondary = (Brush)FindResource("Nyx.SecondaryTextBrush");
        Brush selectedSurface = (Brush)FindResource("Nyx.SelectedBrush");
        Brush border = (Brush)FindResource("Nyx.BorderBrush");

        if (!profile.IsAvailable)
        {
            return new ProfileCardModel(
                profile.Id,
                profile.DisplayName,
                profile.Description,
                BadgeText: _localizer["profiles.card.badge.unavailable"],
                BadgeForeground: muted,
                BadgeBackground: selectedSurface,
                BadgeBorder: border,
                ActionText: _localizer["profiles.card.action.unavailable"],
                IsActionEnabled: false,
                UnavailableReason: profile.UnavailableReason,
                UnavailableReasonVisibility: Visibility.Visible,
                CardOpacity: 0.62);
        }

        if (isCurrent)
        {
            return new ProfileCardModel(
                profile.Id,
                profile.DisplayName,
                profile.Description,
                BadgeText: _localizer["profiles.card.badge.active"],
                BadgeForeground: cyan,
                BadgeBackground: new SolidColorBrush(Color.FromArgb(0x14, 0x30, 0xD1, 0xCB)),
                BadgeBorder: new SolidColorBrush(Color.FromArgb(0x40, 0x30, 0xD1, 0xCB)),
                ActionText: _localizer["profiles.card.action.active"],
                IsActionEnabled: false,
                UnavailableReason: string.Empty,
                UnavailableReasonVisibility: Visibility.Collapsed,
                CardOpacity: 1.0);
        }

        return new ProfileCardModel(
            profile.Id,
            profile.DisplayName,
            profile.Description,
            BadgeText: _localizer["profiles.card.badge.available"],
            BadgeForeground: secondary,
            BadgeBackground: selectedSurface,
            BadgeBorder: border,
            ActionText: _localizer["profiles.card.action.use"],
            IsActionEnabled: true,
            UnavailableReason: string.Empty,
            UnavailableReasonVisibility: Visibility.Collapsed,
            CardOpacity: 1.0);
    }

    private void SetSelectionStatus(string message, bool succeeded)
    {
        SelectionStatusText.Text = message;
        SelectionStatusText.Foreground = succeeded
            ? (Brush)FindResource("Nyx.SuccessBrush")
            : (Brush)FindResource("Nyx.ErrorBrush");
        SelectionStatusText.Visibility = Visibility.Visible;
    }

    private sealed record ProfileCardModel(
        string Id,
        string DisplayName,
        string Description,
        string BadgeText,
        Brush BadgeForeground,
        Brush BadgeBackground,
        Brush BadgeBorder,
        string ActionText,
        bool IsActionEnabled,
        string UnavailableReason,
        Visibility UnavailableReasonVisibility,
        double CardOpacity);
}
