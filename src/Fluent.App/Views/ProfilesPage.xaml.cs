using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fluent.App.Auth;
using Fluent.App.Cloud;
using Fluent.Rewrite.Profiles;
using Fluent.Rewrite.Providers;

namespace Fluent.App.Views;

public partial class ProfilesPage : UserControl
{
    private ProfileSelection? _selection;
    private CloudRewriteSettings? _cloudSettings;
    private IAuthenticationState? _authenticationState;
    private bool _cloudBackendAvailable;
    private string _cloudBackendUnavailableReason = "Le backend Cloud n’est pas configuré.";

    public ProfilesPage()
    {
        InitializeComponent();
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
            ? "Le backend Cloud n’est pas configuré."
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
        string separator = Environment.NewLine + Environment.NewLine;
        string message =
            "Le texte transcrit sera envoye au service Cloud afin d etre reformule." + separator +
            "L audio reste toujours local et n est jamais envoye." + separator +
            "Souhaitez-vous activer la reecriture Cloud ?";

        MessageBoxResult answer = MessageBox.Show(
            message,
            "Consentement a la reecriture Cloud",
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

        CloudToggleButton.Content = enabled ? "Désactiver le Cloud" : "Activer le Cloud";
        CloudToggleButton.IsEnabled = CanActivateCloud(authenticated, _cloudBackendAvailable);
        GeminiProviderButton.Content = provider == RewriteProviderId.Gemini ? "Gemini · actif" : "Gemini";
        DeepSeekProviderButton.Content = provider == RewriteProviderId.DeepSeek ? "DeepSeek · actif" : "DeepSeek V4 Pro";
        GeminiProviderButton.IsEnabled = canSelectProvider && provider != RewriteProviderId.Gemini;
        DeepSeekProviderButton.IsEnabled = canSelectProvider && provider != RewriteProviderId.DeepSeek;

        if (!authenticated)
        {
            CloudBadgeText.Text = "MODE LOCAL";
            CloudStatusText.Text = _authenticationState.Status == AuthenticationStatus.Unconfigured
                ? "Le mode local est utilisé. Configurez l’authentification avant toute option Cloud."
                : "Le mode local est utilisé. Une connexion valide est requise avant toute option Cloud.";
            CloudConsentText.Text =
                "Connexion requise. Tant que vous n'êtes pas connecté, aucun texte ne part vers le Cloud.";
            return;
        }

        if (!_cloudBackendAvailable)
        {
            CloudBadgeText.Text = "SERVICE INDISPONIBLE";
            CloudStatusText.Text = enabled
                ? $"Cloud activé pour cette session avec {providerName}, mais {_cloudBackendUnavailableReason} Le mode local est utilisé."
                : $"Le mode local est utilisé. {_cloudBackendUnavailableReason}";
            CloudConsentText.Text = _cloudSettings.CloudConsentGranted
                ? "Consentement accordé pour cette session. Aucun texte ne part tant que le backend est indisponible."
                : "Le premier usage Cloud demande votre consentement explicite. L’audio reste toujours local.";
            return;
        }

        CloudBadgeText.Text = enabled ? $"CLOUD · {providerName.ToUpperInvariant()} ACTIF" : "MODE LOCAL";
        CloudStatusText.Text = enabled
            ? $"Cloud activé : le texte transcrit est reformulé par {providerName}, avec repli local exact en cas d'échec."
            : "Le mode local est utilisé. Aucune donnée n'est envoyée sur Internet.";
        CloudConsentText.Text = _cloudSettings.CloudConsentGranted
            ? "Consentement accordé pour cette session. L'audio reste toujours local."
            : "Le premier usage Cloud demande votre consentement explicite. L'audio reste toujours local.";
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
            AuthenticationStatus.Authenticated => "CONNECTÉ · GOOGLE",
            AuthenticationStatus.SigningIn => "CONNEXION…",
            AuthenticationStatus.Offline => "SERVICE INDISPONIBLE",
            AuthenticationStatus.Expired => "SESSION EXPIRÉE",
            AuthenticationStatus.Failed => "ÉCHEC DE CONNEXION",
            AuthenticationStatus.Unconfigured => "NON CONFIGURÉ",
            _ => "MODE LOCAL"
        };
        AuthenticationStatusText.Text = _authenticationState.StatusMessage;
        AuthenticationActionButton.Content = authenticated
            ? "Se déconnecter"
            : signingIn
                ? "Connexion en cours"
                : "Se connecter avec Google";
        AuthenticationActionButton.IsEnabled = !signingIn && status != AuthenticationStatus.Unconfigured;
        AuthenticationCancelButton.Visibility = signingIn ? Visibility.Visible : Visibility.Collapsed;

        AuthenticatedUser? user = _authenticationState.User;
        AuthenticatedUserPanel.Visibility = user is null ? Visibility.Collapsed : Visibility.Visible;
        AuthenticatedUserNameText.Text = user?.DisplayName ?? user?.Email ?? "Compte connecté";
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
                BadgeText: "INDISPONIBLE",
                BadgeForeground: muted,
                BadgeBackground: selectedSurface,
                BadgeBorder: border,
                ActionText: "Indisponible",
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
                BadgeText: "ACTIF · ENREGISTRÉ",
                BadgeForeground: cyan,
                BadgeBackground: new SolidColorBrush(Color.FromArgb(0x14, 0x30, 0xD1, 0xCB)),
                BadgeBorder: new SolidColorBrush(Color.FromArgb(0x40, 0x30, 0xD1, 0xCB)),
                ActionText: "Profil actif",
                IsActionEnabled: false,
                UnavailableReason: string.Empty,
                UnavailableReasonVisibility: Visibility.Collapsed,
                CardOpacity: 1.0);
        }

        return new ProfileCardModel(
            profile.Id,
            profile.DisplayName,
            profile.Description,
            BadgeText: "DISPONIBLE",
            BadgeForeground: secondary,
            BadgeBackground: selectedSurface,
            BadgeBorder: border,
            ActionText: "Utiliser ce profil",
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
