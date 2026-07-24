namespace Fluent.Rewrite.Observability;

public interface IRewriteObservabilitySink
{
    void Record(RewriteTelemetry telemetry);
}
