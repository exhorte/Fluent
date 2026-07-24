using Fluent.Persistence;

namespace Fluent.Persistence.Tests;

public sealed class PersistenceAssemblyBoundaryTests
{
    [Fact]
    public void Persistence_project_depends_on_core_boundary()
    {
        Assert.Equal("Fluent.Core", FluentPersistenceAssembly.CoreAssemblyName);
    }
}
