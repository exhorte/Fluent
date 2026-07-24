using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fluent.Rewrite.Dictionary;

namespace Fluent.App.Views;

public partial class DictionaryPage : UserControl
{
    private PersistentPersonalDictionary? _dictionary;
    private IReadOnlyList<PersonalDictionaryEntry> _snapshot = [];
    private CancellationToken _shutdownToken;
    private int _lastPublishedEntryCount = -1;
    private DictionaryStorageMode? _lastPublishedStorageMode;
    private bool _isMutationPending;

    public DictionaryPage()
    {
        InitializeComponent();
    }

    public event EventHandler<int>? EntryCountChanged;

    public event EventHandler<DictionaryStorageMode>? StorageModeChanged;

    public DictionaryStorageMode StorageMode =>
        _dictionary?.StorageMode ?? DictionaryStorageMode.Loading;

    public void Initialize(
        PersistentPersonalDictionary dictionary,
        CancellationToken shutdownToken)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        _dictionary = dictionary;
        _shutdownToken = shutdownToken;
        RefreshStorageState();
        RefreshEntries();
    }

    public async Task LoadAsync()
    {
        if (_dictionary is null)
        {
            SetMutationStatus("Le dictionnaire local n’est pas disponible.", succeeded: false);
            return;
        }

        await _dictionary.InitializeAsync(_shutdownToken);
        RefreshStorageState();
        RefreshEntries();
    }

    private async void AddOrUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_dictionary is null)
        {
            SetMutationStatus("Le dictionnaire local n’est pas disponible.", succeeded: false);
            return;
        }

        SetMutationPending(true);
        try
        {
            DictionaryMutationResult result = await _dictionary.AddOrUpdateAsync(
                SpokenFormTextBox.Text,
                ReplacementTextBox.Text,
                _shutdownToken);

            SetMutationStatus(result.Message, result.Succeeded);
            RefreshStorageState();
            RefreshEntries();
            if (!result.Succeeded)
            {
                return;
            }

            ClearEditor();
            SpokenFormTextBox.Focus();
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
        if (_dictionary is null || sender is not Button { Tag: EntryRow row })
        {
            return;
        }

        SetMutationPending(true);
        try
        {
            DictionaryMutationResult result = await _dictionary.RemoveAsync(
                row.SpokenForm,
                _shutdownToken);

            SetMutationStatus(result.Message, result.Succeeded);
            RefreshStorageState();
            RefreshEntries();
            if (!result.Succeeded)
            {
                return;
            }

            if (string.Equals(
                    SpokenFormTextBox.Text,
                    row.SpokenForm,
                    StringComparison.OrdinalIgnoreCase))
            {
                ClearEditor();
            }
        }
        catch (OperationCanceledException) when (_shutdownToken.IsCancellationRequested)
        {
        }
        finally
        {
            SetMutationPending(false);
        }
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: EntryRow row })
        {
            return;
        }

        SpokenFormTextBox.Text = row.SpokenForm;
        ReplacementTextBox.Text = row.Replacement;
        MutationStatusText.Visibility = Visibility.Collapsed;
        SpokenFormTextBox.Focus();
        SpokenFormTextBox.SelectAll();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplySearchFilter();
    }

    private void RefreshEntries()
    {
        if (_dictionary is null)
        {
            return;
        }

        _snapshot = [.. _dictionary.CreateSnapshot()];
        int count = _snapshot.Count;
        EntryCountTextBlock.Text = StorageMode switch
        {
            DictionaryStorageMode.Persistent when count == 1 => "1 entrée locale enregistrée",
            DictionaryStorageMode.Persistent => $"{count} entrées locales enregistrées",
            _ when count == 1 => "1 entrée pour cette session",
            _ => $"{count} entrées pour cette session"
        };

        if (_lastPublishedEntryCount != count)
        {
            _lastPublishedEntryCount = count;
            EntryCountChanged?.Invoke(this, count);
        }

        ApplySearchFilter();
    }

    private void RefreshStorageState()
    {
        DictionaryStorageMode mode = StorageMode;
        Brush statusBrush = mode switch
        {
            DictionaryStorageMode.Persistent => (Brush)FindResource("Nyx.SuccessBrush"),
            DictionaryStorageMode.SessionOnlyFallback => (Brush)FindResource("Nyx.ErrorBrush"),
            _ => (Brush)FindResource("Nyx.CyanBrush")
        };

        StorageBadgeDot.Fill = statusBrush;
        StorageBadgeTextBlock.Foreground = statusBrush;
        StorageBadgeBorder.BorderBrush = statusBrush;

        switch (mode)
        {
            case DictionaryStorageMode.Persistent:
                StorageBadgeTextBlock.Text = "LOCAL · ENREGISTRÉ";
                StorageDescriptionTextBlock.Text =
                    "Les corrections sont enregistrées uniquement sur cet appareil. " +
                    "Aucun audio, texte dicté, historique ou compte n’est enregistré.";
                break;
            case DictionaryStorageMode.SessionOnlyFallback:
                StorageBadgeTextBlock.Text = "SESSION · NON ENREGISTRÉ";
                StorageDescriptionTextBlock.Text =
                    _dictionary?.StatusMessage ??
                    "Le stockage local est indisponible. Les corrections restent limitées à cette session.";
                break;
            default:
                StorageBadgeTextBlock.Text = "CHARGEMENT LOCAL";
                StorageDescriptionTextBlock.Text =
                    "Initialisation du stockage local du dictionnaire. " +
                    "Aucun audio ni texte dicté n’est enregistré.";
                break;
        }

        SetInteractionState();
        if (_lastPublishedStorageMode != mode)
        {
            _lastPublishedStorageMode = mode;
            StorageModeChanged?.Invoke(this, mode);
        }
    }

    private void ApplySearchFilter()
    {
        string query = SearchTextBox.Text.Trim();
        EntryRow[] visibleEntries = _snapshot
            .Where(entry => MatchesQuery(entry, query))
            .Select(entry => new EntryRow(entry.SpokenForm, entry.Replacement))
            .ToArray();

        EntriesItemsControl.ItemsSource = visibleEntries;
        EntriesItemsControl.Visibility = visibleEntries.Length == 0
            ? Visibility.Collapsed
            : Visibility.Visible;
        EmptyStateBorder.Visibility = visibleEntries.Length == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (visibleEntries.Length != 0)
        {
            return;
        }

        if (StorageMode == DictionaryStorageMode.Loading)
        {
            EmptyStateTitleTextBlock.Text = "Chargement du dictionnaire";
            EmptyStateDescriptionTextBlock.Text =
                "Le stockage local est en cours d’initialisation.";
            return;
        }

        if (_snapshot.Count == 0)
        {
            EmptyStateTitleTextBlock.Text = StorageMode == DictionaryStorageMode.Persistent
                ? "Aucune correction locale"
                : "Aucune correction dans cette session";
            EmptyStateDescriptionTextBlock.Text =
                "Ajoutez une forme reconnue et son remplacement pour commencer.";
            return;
        }

        EmptyStateTitleTextBlock.Text = "Aucun résultat";
        EmptyStateDescriptionTextBlock.Text =
            "Aucune correction ne correspond à cette recherche.";
    }

    private static bool MatchesQuery(PersonalDictionaryEntry entry, string query)
    {
        return query.Length == 0
            || entry.SpokenForm.Contains(query, StringComparison.OrdinalIgnoreCase)
            || entry.Replacement.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void SetMutationPending(bool isPending)
    {
        _isMutationPending = isPending;
        SetInteractionState();
    }

    private void SetInteractionState()
    {
        bool isEnabled =
            StorageMode != DictionaryStorageMode.Loading &&
            !_isMutationPending;
        EditorPanel.IsEnabled = isEnabled;
        SearchTextBox.IsEnabled = isEnabled;
    }

    private void ClearEditor()
    {
        SpokenFormTextBox.Clear();
        ReplacementTextBox.Clear();
    }

    private void SetMutationStatus(string message, bool succeeded)
    {
        MutationStatusText.Text = string.IsNullOrWhiteSpace(message)
            ? succeeded ? "La correction a été mise à jour." : "La correction n’a pas été modifiée."
            : message;
        MutationStatusText.Foreground = succeeded
            ? (Brush)FindResource("Nyx.SuccessBrush")
            : (Brush)FindResource("Nyx.ErrorBrush");
        MutationStatusText.Visibility = Visibility.Visible;
    }

    private sealed record EntryRow(string SpokenForm, string Replacement);
}
