using Fluent.Core;

namespace Fluent.Core.Tests;

public sealed class ProductPrinciplesTests
{
    [Fact]
    public void All_non_negotiable_principles_are_versioned_in_core()
    {
        Assert.Equal(18, FluentPrinciples.All.Count);
        Assert.Equal(FluentPrinciples.All.Count, FluentPrinciples.All.Select(principle => principle.Id).Distinct().Count());
    }

    [Fact]
    public void Privacy_and_safety_principles_are_explicit()
    {
        string joined = string.Join(" ", FluentPrinciples.All.Select(principle => principle.Statement));

        Assert.Contains("No audio is saved by default.", joined);
        Assert.Contains("No telemetry.", joined);
        Assert.Contains("password field", joined);
        Assert.Contains("Never send Enter", joined);
    }
}
