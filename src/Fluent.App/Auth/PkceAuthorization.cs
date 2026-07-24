using System.Security.Cryptography;
using System.Text;

namespace Fluent.App.Auth;

/// <summary>
/// One in-memory OAuth PKCE S256 attempt. The verifier is never persisted or logged.
/// </summary>
public sealed record PkceAuthorization(string Verifier, string Challenge)
{
    public static PkceAuthorization Create()
    {
        string verifier = CreateRandomBase64Url(64);
        byte[] digest = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        string challenge = Base64UrlEncode(digest);
        return new PkceAuthorization(verifier, challenge);
    }

    private static string CreateRandomBase64Url(int length)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(length);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
