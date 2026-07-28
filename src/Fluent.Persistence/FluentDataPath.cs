namespace Fluent.Persistence;

public static class FluentDataPath
{
    private const string ApplicationDirectoryName = "Fluent";
    private const string DatabaseFileName = "fluent.db";
    private const string HistoryDatabaseFileName = "fluent-history.db";
    private const string SettingsDatabaseFileName = "fluent-settings.db";

    public static string GetDefaultDatabasePath()
    {
        return GetDatabasePath(GetLocalApplicationDataRoot());
    }

    public static string GetDatabasePath(string localApplicationDataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationDataPath);

        return Path.Combine(
            Path.GetFullPath(localApplicationDataPath),
            ApplicationDirectoryName,
            DatabaseFileName);
    }

    /// <summary>
    /// Dictation history lives in its own database so it never affects the
    /// single-table dictionary database (<c>fluent.db</c>).
    /// </summary>
    public static string GetDefaultHistoryDatabasePath()
    {
        return GetHistoryDatabasePath(GetLocalApplicationDataRoot());
    }

    public static string GetHistoryDatabasePath(string localApplicationDataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationDataPath);

        return Path.Combine(
            Path.GetFullPath(localApplicationDataPath),
            ApplicationDirectoryName,
            HistoryDatabaseFileName);
    }

    /// <summary>
    /// Local application preferences live in their own database, isolated from
    /// the dictionary and history databases.
    /// </summary>
    public static string GetDefaultSettingsDatabasePath()
    {
        return GetSettingsDatabasePath(GetLocalApplicationDataRoot());
    }

    public static string GetSettingsDatabasePath(string localApplicationDataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationDataPath);

        return Path.Combine(
            Path.GetFullPath(localApplicationDataPath),
            ApplicationDirectoryName,
            SettingsDatabaseFileName);
    }

    private static string GetLocalApplicationDataRoot()
    {
        string localApplicationDataPath = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);

        if (string.IsNullOrWhiteSpace(localApplicationDataPath))
        {
            throw new InvalidOperationException(
                "The local application-data directory is unavailable.");
        }

        return localApplicationDataPath;
    }
}
