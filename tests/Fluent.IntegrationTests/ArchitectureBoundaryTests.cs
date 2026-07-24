using Fluent.Core;

namespace Fluent.IntegrationTests;

public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void Core_does_not_reference_windows_or_wpf_assemblies()
    {
        string[] forbiddenPrefixes =
        [
            "Fluent.Windows",
            "Fluent.App",
            "PresentationFramework",
            "WindowsBase"
        ];

        string[] references = typeof(FluentCoreAssembly)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .ToArray();

        foreach (string prefix in forbiddenPrefixes)
        {
            Assert.DoesNotContain(references, reference => reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
