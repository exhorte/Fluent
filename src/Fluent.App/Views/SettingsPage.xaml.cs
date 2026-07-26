using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fluent.Rewrite.Profiles;

namespace Fluent.App.Views;

/// <summary>
/// Local settings page. Surfaces reversible local preferences (default rewrite
/// profile, history summary, storage/privacy facts). Code-behind is visual-only;
/// persistence is owned by the host through events. No secret is shown here.
/// </summary>
public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    public event EventHandler<string>? DefaultProfileChangeRequested;

    public event EventHandler? OpenHistoryRequested;

    public event EventHandler<string>? LanguageChangeRequested;

    public event EventHandler<string>? TranscriptionLanguageChangeRequested;

    public void SetProfiles(IReadOnlyList<RewriteProfile> profiles, RewriteProfile current)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(current);

        ProfileOption[] options = profiles
            .Where(profile => profile.IsAvailable)
            .Select(profile =>
            {
                bool isCurrent = string.Equals(profile.Id, current.Id, StringComparison.Ordinal);
                return new ProfileOption(
                    profile.Id,
                    profile.DisplayName,
                    isCurrent ? "Profil par défaut" : "Définir par défaut",
                    !isCurrent);
            })
            .ToArray();

        DefaultProfileItemsControl.ItemsSource = options;
        CurrentProfileText.Text = current.DisplayName;
    }

    public void SetSessionInfo(string profileName, string modeLabel, string engine)
    {
        SessionProfileText.Text = profileName;
        SessionModeText.Text = modeLabel;
        SessionEngineText.Text = engine;
    }

    public void SetLanguageSelection(string language)
    {
        bool fr = language == "fr";
        EnglishLanguageButton.Content = fr ? "English" : "✓ English";
        FrenchLanguageButton.Content = fr ? "✓ Français" : "Français";
    }

    public void SetTranscriptionLanguageSelection(string mode)
    {
        TranscriptionAutoButton.Content = mode == "auto" ? "✓ " + AutoLabel : AutoLabel;
        TranscriptionFrButton.Content = mode == "fr" ? "✓ " + FrLabel : FrLabel;
        TranscriptionEnButton.Content = mode == "en" ? "✓ " + EnLabel : EnLabel;
    }

    private string AutoLabel => "Auto"; // dynamically set in code-behind for now
    private string FrLabel => "Français";
    private string EnLabel => "English";

    private void EnglishLanguageButton_Click(object sender, RoutedEventArgs e)
    {
        LanguageChangeRequested?.Invoke(this, "en");
    }

    private void FrenchLanguageButton_Click(object sender, RoutedEventArgs e)
    {
        LanguageChangeRequested?.Invoke(this, "fr");
    }

    private void TranscriptionAutoButton_Click(object sender, RoutedEventArgs e)
    {
        TranscriptionLanguageChangeRequested?.Invoke(this, "auto");
    }

    private void TranscriptionFrButton_Click(object sender, RoutedEventArgs e)
    {
        TranscriptionLanguageChangeRequested?.Invoke(this, "fr");
    }

    private void TranscriptionEnButton_Click(object sender, RoutedEventArgs e)
    {
        TranscriptionLanguageChangeRequested?.Invoke(this, "en");
    }

    public void SetHistorySummary(bool enabled, int count)
    {
        Brush success = (Brush)FindResource("Nyx.SuccessBrush");
        Brush muted = (Brush)FindResource("Nyx.MutedTextBrush");

        HistoryStateText.Text = enabled ? "Activé" : "Désactivé";
        HistoryStateText.Foreground = enabled ? success : muted;
        HistoryDetailText.Text = enabled
            ? count switch
            {
                0 => "Aucune dictée enregistrée pour l’instant.",
                1 => "1 dictée enregistrée localement.",
                _ => $"{count} dictées enregistrées localement."
            }
            : "L’historique local est désactivé. Aucune dictée n’est enregistrée.";
    }

    private void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string profileId })
        {
            DefaultProfileChangeRequested?.Invoke(this, profileId);
        }
    }

    private void OpenHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        OpenHistoryRequested?.Invoke(this, EventArgs.Empty);
    }

    private sealed record ProfileOption(
        string Id,
        string DisplayName,
        string ActionText,
        bool IsActionEnabled);
}
