using Fluent.Rewrite;

namespace Fluent.Rewrite.Tests;

public sealed class RewriteAssemblyBoundaryTests
{
    [Fact]
    public void Rewrite_project_depends_on_core_boundary()
    {
        Assert.Equal("Fluent.Core", FluentRewriteAssembly.CoreAssemblyName);
    }
}
