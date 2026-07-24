namespace Fluent.IntegrationTests;

public sealed class DeepSeekSecurityScanTests
{
    [Fact]
    public void Desktop_projects_have_no_deepseek_configuration_or_endpoint()
    {
        string root = RepositoryRoot();
        string desktopSource = ReadSource(
            Path.Combine(root, "src", "Fluent.App", "Cloud"),
            Path.Combine(root, "src", "Fluent.Cloud"),
            Path.Combine(root, "src", "Fluent.Rewrite"));

        Assert.DoesNotContain("DEEPSEEK_API_KEY", desktopSource, StringComparison.Ordinal);
        Assert.DoesNotContain("DEEPSEEK_MODEL", desktopSource, StringComparison.Ordinal);
        Assert.DoesNotContain("DEEPSEEK_BASE_URL", desktopSource, StringComparison.Ordinal);
        Assert.DoesNotContain("api.deepseek.com", desktopSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("File.Read", desktopSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Backend_deepseek_transport_uses_redacted_http_and_no_file_secret_loader()
    {
        string root = RepositoryRoot();
        string transport = File.ReadAllText(Path.Combine(
            root, "src", "Fluent.Backend", "Rewriting", "HttpDeepSeekApi.cs"));
        string program = File.ReadAllText(Path.Combine(root, "src", "Fluent.Backend", "Program.cs"));

        Assert.Contains("https://api.deepseek.com/chat/completions", transport, StringComparison.Ordinal);
        Assert.Contains("Authorization", transport, StringComparison.Ordinal);
        Assert.DoesNotContain("?api", transport, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("File.Read", transport, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.", transport, StringComparison.Ordinal);
        Assert.DoesNotContain("ILogger", transport, StringComparison.Ordinal);
        Assert.Contains("AddHttpClient<IDeepSeekApi, HttpDeepSeekApi>(client => client.Timeout = TimeSpan.FromSeconds(8)).RemoveAllLoggers()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("BadRequest(new { error = \"provider_unavailable\" })", program, StringComparison.Ordinal);
        Assert.Contains("return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);", program, StringComparison.Ordinal);
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Fluent.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
    }

    private static string ReadSource(params string[] directories) =>
        string.Join(
            Environment.NewLine,
            directories.SelectMany(directory => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(File.ReadAllText));
}
