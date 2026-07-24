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
