using System.ComponentModel;
using System.Windows.Data;

namespace Fluent.App.Localization;

/// <summary>
/// Observable localization source bound from XAML as
/// <c>{Binding [key], Source={StaticResource Loc}}</c>. Changing
/// <see cref="Language"/> refreshes every bound string at once. Default: English.
/// </summary>
public sealed class Localizer : INotifyPropertyChanged
{
    private string _language = "en";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Language
    {
        get => _language;
        set
        {
            string normalized = value == "fr" ? "fr" : "en";
            if (_language == normalized)
            {
                return;
            }

            _language = normalized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
        }
    }

    /// <summary>Localized string for a key; falls back to English, then the key.</summary>
    public string this[string key]
    {
        get
        {
            if (LocalizedStrings.Catalog.TryGetValue(key, out (string En, string Fr) value))
            {
                return _language == "fr" ? value.Fr : value.En;
            }

            return key;
        }
    }
}
