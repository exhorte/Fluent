namespace Fluent.Persistence;

public static class FluentDataPath
{
    private const string ApplicationDirectoryName = "Fluent";
    private const string DatabaseFileName = "fluent.db";

    public static string GetDefaultDatabasePath()
    {
        string localApplicationDataPath = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);

        if (string.IsNullOrWhiteSpace(localApplicationDataPath))
        {
            throw new InvalidOperationException(
                "The local application-data directory is unavailable.");
        }

        return GetDatabasePath(localApplicationDataPath);
    }

    public static string GetDatabasePath(string localApplicationDataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationDataPath);

        return Path.Combine(
            Path.GetFullPath(localApplicationDataPath),
            ApplicationDirectoryName,
            DatabaseFileName);
    }
}
