using Fluent.Audio;

namespace Fluent.Audio.Tests;

public sealed class AudioAssemblyBoundaryTests
{
    [Fact]
    public void Audio_project_depends_on_core_boundary()
    {
        Assert.Equal("Fluent.Core", FluentAudioAssembly.CoreAssemblyName);
    }
}
