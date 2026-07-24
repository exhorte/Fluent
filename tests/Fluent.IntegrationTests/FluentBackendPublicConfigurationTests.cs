using Fluent.App.Cloud;
using Fluent.App.Views;

namespace Fluent.IntegrationTests;

public sealed class FluentBackendPublicConfigurationTests
{
    [Theory]
    [InlineData("https://backend.fluent.example", "https://backend.fluent.example/")]
    [InlineData("https://backend.fluent.example/", "https://backend.fluent.example/")]
    public void Valid_https_root_origin_is_normalized(
        string backendUrl,
        string expectedOrigin)
    {
        bool created = FluentBackendPublicConfiguration.TryCreate(
            backendUrl,
            out FluentBackendPublicConfiguration? configuration,
            out string unavailableReason);

        Assert.True(created);
        Assert.NotNull(configuration);
        Assert.Equal(expectedOrigin, configuration.Origin.AbsoluteUri);
        Assert.Equal(string.Empty, unavailableReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://backend.fluent.example")]
    [InlineData("/v1/rewrite")]
    [InlineData("https://user@backend.fluent.example")]
    [InlineData("https://backend.fluent.example?next=value")]
    [InlineData("https://backend.fluent.example#fragment")]
    [InlineData("https://backend.fluent.example/api")]
    [InlineData("https://backend.fluent.example:8443")]
    public void Missing_or_unsafe_origin_is_rejected(string? backendUrl)
    {
        bool created = FluentBackendPublicConfiguration.TryCreate(
            backendUrl,
            out FluentBackendPublicConfiguration? configuration,
            out string unavailableReason);

        Assert.False(created);
        Assert.Null(configuration);
        Assert.NotEmpty(unavailableReason);
    }

    [Fact]
    public void Process_environment_is_the_only_configuration_input_and_is_restored()
    {
        using EnvironmentVariableScope scope = new(
            FluentBackendPublicConfiguration.BackendUrlEnvironmentVariable,
            "https://backend.fluent.example");

        bool loaded = FluentBackendPublicConfiguration.TryLoadFromEnvironment(
            out FluentBackendPublicConfiguration? configuration,
            out string unavailableReason);

        Assert.True(loaded);
        Assert.NotNull(configuration);
        Assert.Equal("https://backend.fluent.example/", configuration.Origin.AbsoluteUri);
        Assert.Equal(string.Empty, unavailableReason);
    }

    [Fact]
    public void Configuration_source_does_not_load_dotenv_or_provider_material()
    {
        string source = File.ReadAllText(SourcePath());

        Assert.DoesNotContain("File.Read", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ConfigurationBuilder", source, StringComparison.Ordinal);
        Assert.DoesNotContain("dotenv", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GEMINI", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DEEPSEEK", source, StringComparison.Ordinal);
        Assert.Contains("GetEnvironmentVariable", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void Cloud_activation_requires_an_authenticated_user_and_configured_backend_origin(
        bool authenticated,
        bool hasConfiguredBackendOrigin,
        bool expected)
    {
        Assert.Equal(
            expected,
            ProfilesPage.CanActivateCloud(authenticated, hasConfiguredBackendOrigin));
    }

    private static string SourcePath()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Fluent.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(
            directory!.FullName,
            "src",
            "Fluent.App",
            "Cloud",
            "FluentBackendPublicConfiguration.cs");
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _name;
        private readonly string? _previousValue;

        public EnvironmentVariableScope(string name, string value)
        {
            _name = name;
            _previousValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_name, _previousValue);
        }
    }
}
