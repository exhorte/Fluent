using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fluent.Core.History;

namespace Fluent.App.Views;

/// <summary>
/// Local dictation-history page. History is opt-in (disabled by default); no
/// audio is ever stored. Code-behind is visual-only: all persistence lives in
/// the injected <see cref="IDictationHistoryStore"/> and the pure capture policy.
/// </summary>
public partial class HistoryPage : UserControl
{
    private IDictationHistoryStore? _store;
    private CancellationToken _shutdownToken;
    private DictationHistoryPreferences _preferences = DictationHistoryPreferences.Disabled;
    private IReadOnlyList<DictationHistoryEntry> _entries = [];
    private int _lastPublishedCount = -1;
    private bool _isMutationPending;

    public HistoryPage()
    {
        InitializeComponent();
    }

    public event EventHandler<int>? EntryCountChanged;

    public event EventHandler<bool>? EnabledChanged;

    public bool HistoryEnabled => _preferences.IsEnabled;

    public void Initialize(
        IDictationHistoryStore store,
        CancellationToken shutdownToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _shutdownToken = shutdownToken;
    }

    public async Task LoadAsync()
    {
        if (_store is null)
        {
            SetStatus("L’historique local n’est pas disponible.", succeeded: false);
            return;
        }

        try
        {
            await ReloadAsync();
        }
        catch (OperationCanceledException) when (_shutdownToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>
    /// Applies the opt-in capture policy to a completed dictation and records it
    /// when enabled. Never throws into the dictation flow.
    /// </summary>
    public async Task CaptureAsync(string dictatedText, string? profileId)
    {
        if (_store is null)
        {
            return;
        }

        DictationHistoryCaptureDecision decision = DictationHistoryCapturePolicy.Decide(
            _preferences,
            dictatedText,
            profileId,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        if (!decision.ShouldRecord || decision.Entry is null)
        {
            return;
        }

        try
        {
            await _store.AppendAsync(decision.Entry, _shutdownToken);
            await ReloadAsync();
        }
        catch (OperationCanceledException) when (_shutdownToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            SetStatus(
                $"L’enregistrement de l’historique a échoué : {ex.GetBaseException().Message}",
                succeeded: false);
        }
    }

    private async void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_store is null || _isMutationPending)
        {
            return;
        }

        bool desired = !_preferences.IsEnabled;
        SetMutationPending(true);
        try
        {
            await _store.SetEnabledAsync(desired, _shutdownToken);
            _preferences = new DictationHistoryPreferences(desired);
            RefreshToggle();
            SetStatus(
                desired
                    ? "Historique activé. Les prochaines dictées seront enregistrées localement."
                    : "Historique désactivé. Aucune nouvelle dictée n’est enregistrée.",
                succeeded: true);
        }
        catch (OperationCanceledException) when (_shutdownToken.IsCancellationRequested)
        {
        }
        finally
        {
            SetMutationPending(false);
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_store is null || sender is not Button { Tag: HistoryRow row })
        {
            return;
        }

        SetMutationPending(true);
        try
        {
            await _store.DeleteAsync(row.Id, _shutdownToken);
            await ReloadAsync();
        }
        catch (OperationCanceledException) when (_shutdownToken.IsCancellationRequested)
        {
        }
        finally
        {
            SetMutationPending(false);
        }
    }

    private async void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        if (_store is null || _isMutationPending)
        {
            return;
        }

        SetMutationPending(true);
        try
        {
            int removed = await _store.ClearAsync(_shutdownToken);
            await ReloadAsync();
            SetStatus(
                removed == 0
                    ? "L’historique était déjà vide."
                    : removed == 1
                        ? "1 entrée supprimée."
                        : $"{removed} entrées supprimées.",
                succeeded: true);
        }
        catch (OperationCanceledException) when (_shutdownToken.IsCancellationRequested)
        {
        }
        finally
        {
            SetMutationPending(false);
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshList();
    }

    private async Task ReloadAsync()
    {
        if (_store is null)
        {
            return;
        }

        DictationHistorySnapshot snapshot =
            await _store.InitializeAndLoadAsync(_shutdownToken);
        _preferences = snapshot.Preferences;
        _entries = snapshot.Entries;
        RefreshToggle();
        RefreshList();
        PublishCount();
    }

    private void RefreshToggle()
    {
        bool enabled = _preferences.IsEnabled;
        Brush brush = enabled
            ? (Brush)FindResource("Nyx.SuccessBrush")
            : (Brush)FindResource("Nyx.MutedTextBrush");

        StateBadgeDot.Fill = brush;
        StateBadgeText.Foreground = brush;
        StateBadgeBorder.BorderBrush = brush;
        StateBadgeText.Text = enabled ? "ACTIVÉ" : "DÉSACTIVÉ";
        ToggleButton.Content = enabled ? "Désactiver l’historique" : "Activer l’historique";

        EnabledChanged?.Invoke(this, enabled);
    }

    private void RefreshList()
    {
        string query = SearchTextBox.Text.Trim();
        HistoryRow[] rows = _entries
            .Where(entry => MatchesQuery(entry, query))
            .Select(ToRow)
            .ToArray();

        EntriesItemsControl.ItemsSource = rows;
        EntriesItemsControl.Visibility = rows.Length == 0
            ? Visibility.Collapsed
            : Visibility.Visible;
        EmptyStateBorder.Visibility = rows.Length == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        EntryCountTextBlock.Text = _entries.Count switch
        {
            0 => "Aucune dictée enregistrée",
            1 => "1 dictée enregistrée localement",
            _ => $"{_entries.Count} dictées enregistrées localement"
        };

        if (rows.Length != 0)
        {
            return;
        }

        if (!_preferences.IsEnabled)
        {
            EmptyStateTitleTextBlock.Text = "Historique désactivé";
            EmptyStateDescriptionTextBlock.Text =
                "Activez l’historique pour enregistrer localement le texte de vos prochaines dictées.";
            return;
        }

        if (_entries.Count == 0)
        {
            EmptyStateTitleTextBlock.Text = "Aucune dictée enregistrée";
            EmptyStateDescriptionTextBlock.Text =
                "Vos prochaines dictées apparaîtront ici. Rien n’est envoyé à l’extérieur.";
            return;
        }

        EmptyStateTitleTextBlock.Text = "Aucun résultat";
        EmptyStateDescriptionTextBlock.Text =
            "Aucune dictée ne correspond à cette recherche.";
    }

    private static bool MatchesQuery(DictationHistoryEntry entry, string query)
    {
        return query.Length == 0
            || entry.Text.Contains(query, StringComparison.OrdinalIgnoreCase)
            || (entry.ProfileId is not null
                && entry.ProfileId.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static HistoryRow ToRow(DictationHistoryEntry entry)
    {
        string time = entry.CreatedUtc.ToLocalTime()
            .ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
        string profile = string.IsNullOrWhiteSpace(entry.ProfileId)
            ? "—"
            : entry.ProfileId!;
        return new HistoryRow(entry.Id, time, entry.Text, profile);
    }

    private void PublishCount()
    {
        if (_lastPublishedCount == _entries.Count)
        {
            return;
        }

        _lastPublishedCount = _entries.Count;
        EntryCountChanged?.Invoke(this, _entries.Count);
    }

    private void SetMutationPending(bool isPending)
    {
        _isMutationPending = isPending;
        ToggleButton.IsEnabled = !isPending;
        ClearButton.IsEnabled = !isPending;
        SearchTextBox.IsEnabled = !isPending;
    }

    private void SetStatus(string message, bool succeeded)
    {
        StatusText.Text = message;
        StatusText.Foreground = succeeded
            ? (Brush)FindResource("Nyx.SuccessBrush")
            : (Brush)FindResource("Nyx.ErrorBrush");
        StatusText.Visibility = Visibility.Visible;
    }

    private sealed record HistoryRow(Guid Id, string Time, string Text, string Profile);
}
