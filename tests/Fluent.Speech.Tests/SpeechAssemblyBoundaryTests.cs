using Fluent.Speech;

namespace Fluent.Speech.Tests;

public sealed class SpeechAssemblyBoundaryTests
{
    [Fact]
    public void Speech_project_depends_on_core_boundary()
    {
        Assert.Equal("Fluent.Core", FluentSpeechAssembly.CoreAssemblyName);
    }
}
