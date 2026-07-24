namespace Fluent.Rewrite.Rewriting;

public sealed class DeveloperPassThroughRewriter : ILocalTextRewriter
{
    public Task<string> RewriteAsync(
        RewriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(request.Text);
    }
}
