namespace Fluent.IntegrationTests;

public sealed class AuthenticationSecurityScanTests
{
    [Fact]
    public void Desktop_authentication_has_no_embedded_browser_or_file_based_secret_loader()
    {
        string root = RepositoryRoot();
        string authDirectory = Path.Combine(root, "src", "Fluent.App", "Auth");
        string source = ReadSource(authDirectory);

        Assert.DoesNotContain("WebView", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Cef", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("File.Read", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DEEPSEEK", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GEMINI_API_KEY", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Refresh_store_cannot_persist_access_token_and_auth_code_paths_do_not_log()
    {
        string root = RepositoryRoot();
        string refreshStore = File.ReadAllText(Path.Combine(
            root, "src", "Fluent.App", "Auth", "RefreshTokenStore.cs"));
        string authSource = ReadSource(Path.Combine(root, "src", "Fluent.App", "Auth"));

        Assert.DoesNotContain("AccessToken", refreshStore, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.", authSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ILogger", authSource, StringComparison.Ordinal);
        Assert.DoesNotContain("(\"state\",", authSource, StringComparison.Ordinal);
        Assert.DoesNotContain("StateMatches", authSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Backend_route_no_longer_contains_static_token_authentication()
    {
        string root = RepositoryRoot();
        string backend = ReadSource(Path.Combine(root, "src", "Fluent.Backend"));

        Assert.DoesNotContain("FLUENT_BACKEND_TOKEN", backend, StringComparison.Ordinal);
        Assert.DoesNotContain("BackendAuthenticator", backend, StringComparison.Ordinal);
        Assert.Contains("SupabaseJwtValidator", backend, StringComparison.Ordinal);
        Assert.Contains("ValidatedUserRateLimiter", backend, StringComparison.Ordinal);
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

    private static string ReadSource(string directory)
    {
        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(File.ReadAllText));
    }
}
