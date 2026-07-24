using Fluent.App.Auth;

namespace Fluent.App.Cloud;

/// <summary>
/// Default unauthenticated state. Replace via a future authentication phase; nothing here
/// stores or reads a secret.
/// </summary>
public sealed class DefaultAuthenticationState : IAuthenticationState
{
    public event EventHandler? Changed
    {
        add { }
        remove { }
    }

    public bool IsAuthenticated => false;

    public string? AccessToken => null;

    public AuthenticationStatus Status => AuthenticationStatus.Unconfigured;

    public AuthenticatedUser? User => null;

    public bool IsOperationInProgress => false;

    public string StatusMessage => "Authentification non configurée ; le mode local reste actif.";

    public Task RestoreSessionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SignInWithGoogleAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void CancelSignIn()
    {
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
