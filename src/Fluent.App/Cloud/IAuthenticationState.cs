using Fluent.App.Auth;

namespace Fluent.App.Cloud;

/// <summary>
/// Session seam used by the Cloud gate and the profile presentation. Implementations expose no
/// refresh token and never persist an access token.
/// </summary>
public interface IAuthenticationState
{
    event EventHandler? Changed;

    bool IsAuthenticated { get; }

    string? AccessToken { get; }

    AuthenticationStatus Status { get; }

    AuthenticatedUser? User { get; }

    bool IsOperationInProgress { get; }

    string StatusMessage { get; }

    Task RestoreSessionAsync(CancellationToken cancellationToken = default);

    Task SignInWithGoogleAsync(CancellationToken cancellationToken = default);

    void CancelSignIn();

    Task SignOutAsync(CancellationToken cancellationToken = default);
}
