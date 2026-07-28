using System.Windows;
using System.Windows.Controls;
using Fluent.App.Localization;

namespace Fluent.App.Views;

/// <summary>
/// Local subscription page: current plan, account, and a Pro upgrade call to
/// action. No payment data is collected here; a real purchase is a separate,
/// user-authorised step.
/// </summary>
public partial class SubscriptionPage : UserControl
{
    public SubscriptionPage()
    {
        InitializeComponent();
    }

    public void SetAccount(string accountLabel)
    {
        AccountText.Text = accountLabel;
    }

    private void UpgradeButton_Click(object sender, RoutedEventArgs e)
    {
        UpgradeStatusText.Text =
            ((Localizer)Application.Current.Resources["Loc"])["subscription.upgrade.status"];
        UpgradeStatusText.Visibility = Visibility.Visible;
    }
}
