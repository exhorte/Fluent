namespace Fluent.IntegrationTests;

/// <summary>
/// Deterministic checks that the primary navigation and pages carry accessibility
/// names and a keyboard tab order (07H). These lock in the accessibility surface
/// that a visual smoke cannot guarantee on its own.
/// </summary>
public sealed class AccessibilityMarkupTests
{
    [Theory]
    [InlineData("AutomationProperties.Name=\"Vue d'ensemble\"")]
    [InlineData("AutomationProperties.Name=\"Historique\"")]
    [InlineData("AutomationProperties.Name=\"Dictionnaire\"")]
    [InlineData("AutomationProperties.Name=\"Profils\"")]
    [InlineData("AutomationProperties.Name=\"Paramètres\"")]
    public void Navigation_entries_have_accessibility_names(string expected)
    {
        string markup = File.ReadAllText(SourcePath("src", "Fluent.App", "MainWindow.xaml"));
        Assert.Contains(expected, markup, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("AutomationProperties.Name=\"Page Vue d'ensemble\"")]
    [InlineData("AutomationProperties.Name=\"Page Dictionnaire\"")]
    [InlineData("AutomationProperties.Name=\"Page Profils\"")]
    [InlineData("AutomationProperties.Name=\"Page Historique\"")]
    [InlineData("AutomationProperties.Name=\"Page Paramètres\"")]
    public void Pages_have_accessibility_names(string expected)
    {
        string markup = File.ReadAllText(SourcePath("src", "Fluent.App", "MainWindow.xaml"));
        Assert.Contains(expected, markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Navigation_declares_a_keyboard_tab_order()
    {
        string markup = File.ReadAllText(SourcePath("src", "Fluent.App", "MainWindow.xaml"));
        foreach (string tabIndex in new[] { "TabIndex=\"0\"", "TabIndex=\"1\"", "TabIndex=\"2\"", "TabIndex=\"3\"", "TabIndex=\"4\"" })
        {
            Assert.Contains(tabIndex, markup, StringComparison.Ordinal);
        }
    }

    private static string SourcePath(params string[] segments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Fluent.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine([directory!.FullName, .. segments]);
    }
}
