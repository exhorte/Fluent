namespace Fluent.Rewrite.Rewriting;

public interface ILocalTextRewriter
{
    Task<string> RewriteAsync(
        RewriteRequest request,
        CancellationToken cancellationToken = default);
}
