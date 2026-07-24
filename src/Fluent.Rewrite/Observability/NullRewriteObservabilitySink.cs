namespace Fluent.Rewrite.Observability;

public sealed class NullRewriteObservabilitySink : IRewriteObservabilitySink
{
    public static readonly NullRewriteObservabilitySink Instance = new();

    private NullRewriteObservabilitySink()
    {
    }

    public void Record(RewriteTelemetry telemetry)
    {
    }
}
