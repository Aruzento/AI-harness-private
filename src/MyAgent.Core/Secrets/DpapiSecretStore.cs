using System.Security.Cryptography;
using System.Text;
using System.Runtime.Versioning;

namespace MyAgent.Secrets;

[SupportedOSPlatform("windows")]
public sealed class DpapiSecretStore
    : ISecretStore
{
    private static readonly byte[] AdditionalEntropy =
        Encoding.UTF8.GetBytes(
            "MyAgent.DpapiSecretStore.v1");

    private readonly string _directoryPath;

    public static string DefaultDirectoryPath =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "MyAgent",
            "secrets");

    public DpapiSecretStore(
        string? directoryPath = null)
    {
        _directoryPath =
            string.IsNullOrWhiteSpace(directoryPath)
                ? DefaultDirectoryPath
                : Path.GetFullPath(directoryPath);
    }

    public async Task<string?> GetAsync(
        string secretId,
        CancellationToken cancellationToken = default)
    {
        EnsureSecretId(
            secretId);

        EnsureWindows();

        string filePath =
            GetSecretFilePath(
                secretId);

        if (!File.Exists(filePath))
        {
            return null;
        }

        byte[] encryptedBytes =
            await File.ReadAllBytesAsync(
                filePath,
                cancellationToken);

        cancellationToken
            .ThrowIfCancellationRequested();

        byte[]? plainBytes =
            null;

        try
        {
            plainBytes =
                ProtectedData.Unprotect(
                    encryptedBytes,
                    AdditionalEntropy,
                    DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(
                plainBytes);
        }
        finally
        {
            if (plainBytes is not null)
            {
                CryptographicOperations.ZeroMemory(
                    plainBytes);
            }
        }
    }

    public async Task SetAsync(
        string secretId,
        string value,
        CancellationToken cancellationToken = default)
    {
        EnsureSecretId(
            secretId);

        ArgumentNullException.ThrowIfNull(
            value);

        if (value.Length == 0)
        {
            throw new ArgumentException(
                "Secret value cannot be empty.",
                nameof(value));
        }

        EnsureWindows();

        cancellationToken
            .ThrowIfCancellationRequested();

        Directory.CreateDirectory(
            _directoryPath);

        string filePath =
            GetSecretFilePath(
                secretId);

        string temporaryPath =
            filePath
            + "."
            + Guid.NewGuid().ToString("N")
            + ".tmp";

        byte[] plainBytes =
            Encoding.UTF8.GetBytes(
                value);

        byte[]? encryptedBytes =
            null;

        try
        {
            encryptedBytes =
                ProtectedData.Protect(
                    plainBytes,
                    AdditionalEntropy,
                    DataProtectionScope.CurrentUser);

            await File.WriteAllBytesAsync(
                temporaryPath,
                encryptedBytes,
                cancellationToken);

            cancellationToken
                .ThrowIfCancellationRequested();

            File.Move(
                temporaryPath,
                filePath,
                overwrite: true);
        }
        catch
        {
            if (File.Exists(
                    temporaryPath))
            {
                File.Delete(
                    temporaryPath);
            }

            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                plainBytes);

            if (encryptedBytes is not null)
            {
                CryptographicOperations.ZeroMemory(
                    encryptedBytes);
            }
        }
    }

    public Task<bool> ContainsAsync(
        string secretId,
        CancellationToken cancellationToken = default)
    {
        EnsureSecretId(
            secretId);

        cancellationToken
            .ThrowIfCancellationRequested();

        string filePath =
            GetSecretFilePath(
                secretId);

        return Task.FromResult(
            File.Exists(filePath));
    }

    public Task DeleteAsync(
        string secretId,
        CancellationToken cancellationToken = default)
    {
        EnsureSecretId(
            secretId);

        cancellationToken
            .ThrowIfCancellationRequested();

        string filePath =
            GetSecretFilePath(
                secretId);

        if (File.Exists(filePath))
        {
            File.Delete(
                filePath);
        }

        return Task.CompletedTask;
    }

    private string GetSecretFilePath(
        string secretId)
    {
        byte[] idBytes =
            Encoding.UTF8.GetBytes(
                secretId);

        try
        {
            byte[] hash =
                SHA256.HashData(
                    idBytes);

            string fileName =
                Convert.ToHexString(
                        hash)
                    .ToLowerInvariant()
                + ".bin";

            return Path.Combine(
                _directoryPath,
                fileName);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                idBytes);
        }
    }

    private static void EnsureSecretId(
        string secretId)
    {
        if (string.IsNullOrWhiteSpace(
                secretId))
        {
            throw new ArgumentException(
                "Secret ID cannot be empty.",
                nameof(secretId));
        }
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "DPAPI secret storage is supported only on Windows.");
        }
    }
}