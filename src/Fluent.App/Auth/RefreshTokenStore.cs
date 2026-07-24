using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Fluent.App.Auth;

public interface IRefreshTokenStore
{
    Task<string?> ReadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task DeleteAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Stores the refresh token in a Windows Generic Credential owned by the current Windows user.
/// Access tokens, authorization codes and PKCE values are never written to this store.
/// </summary>
public sealed class WindowsCredentialManagerRefreshTokenStore : IRefreshTokenStore
{
    private const string TargetName = "Fluent.Supabase.RefreshToken.V1";
    private const uint CredentialTypeGeneric = 1;
    private const uint CredentialPersistLocalMachine = 2;
    private const int ErrorNotFound = 1168;
    private const int MaximumCredentialBlobBytes = 2048;

    public Task<string?> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!CredRead(TargetName, CredentialTypeGeneric, 0, out nint credentialPointer))
        {
            int error = Marshal.GetLastWin32Error();
            if (error == ErrorNotFound)
            {
                return Task.FromResult<string?>(null);
            }

            throw new Win32Exception(error, "Lecture du jeton de session impossible.");
        }

        try
        {
            NativeCredential credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
            if (credential.CredentialBlob == nint.Zero
                || credential.CredentialBlobSize == 0
                || credential.CredentialBlobSize > MaximumCredentialBlobBytes
                || credential.CredentialBlobSize % sizeof(char) != 0)
            {
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult<string?>(Marshal.PtrToStringUni(
                credential.CredentialBlob,
                checked((int)credential.CredentialBlobSize / sizeof(char))));
        }
        finally
        {
            CredFree(credentialPointer);
        }
    }

    public Task SaveAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        byte[] payload = MemoryMarshal.AsBytes(refreshToken.AsSpan()).ToArray();
        if (payload.Length > MaximumCredentialBlobBytes)
        {
            throw new InvalidOperationException("Jeton de session trop volumineux.");
        }

        nint blob = Marshal.AllocCoTaskMem(payload.Length);
        try
        {
            Marshal.Copy(payload, 0, blob, payload.Length);
            NativeCredential credential = new()
            {
                Type = CredentialTypeGeneric,
                TargetName = TargetName,
                CredentialBlobSize = checked((uint)payload.Length),
                CredentialBlob = blob,
                Persist = CredentialPersistLocalMachine,
                UserName = "Fluent"
            };

            if (!CredWrite(ref credential, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Enregistrement du jeton de session impossible.");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(payload);
            Marshal.FreeCoTaskMem(blob);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!CredDelete(TargetName, CredentialTypeGeneric, 0))
        {
            int error = Marshal.GetLastWin32Error();
            if (error != ErrorNotFound)
            {
                throw new Win32Exception(error, "Suppression du jeton de session impossible.");
            }
        }

        return Task.CompletedTask;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public nint CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public nint Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredRead(string target, uint type, uint reservedFlag, out nint credential);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWrite(ref NativeCredential userCredential, uint flags);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern void CredFree(nint buffer);
}
