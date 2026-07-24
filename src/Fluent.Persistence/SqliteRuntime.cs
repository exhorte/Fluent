using System.Threading;

namespace Fluent.Persistence;

internal static class SqliteRuntime
{
    private static readonly Lazy<bool> Initializer = new(
        Initialize,
        LazyThreadSafetyMode.ExecutionAndPublication);

    public static void EnsureInitialized()
    {
        _ = Initializer.Value;
    }

    private static bool Initialize()
    {
        SQLitePCL.Batteries_V2.Init();
        return true;
    }
}
