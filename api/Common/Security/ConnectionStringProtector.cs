using System.Security.Cryptography;
using System.Text;

namespace Api.Common.Security;

/// <summary>
/// AES-256-GCM encryption for tenant connection strings.
/// Stored format: "ENC:" + Base64(nonce[12] + tag[16] + ciphertext).
/// If no encryption key is configured in Development, passes through plaintext.
/// On decrypt, strings without the "ENC:" prefix are returned as-is (backward compatible).
/// </summary>
internal sealed class ConnectionStringProtector(
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<ConnectionStringProtector> logger) : IConnectionStringProtector
{
    private const string Prefix = "ENC:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[]? _key = ResolveKey(configuration, environment);

    public string Protect(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return plainText;

        if (_key is null)
        {
            logger.LogDebug("ConnectionString protection skipped — no encryption key configured (dev mode)");
            return plainText;
        }

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var cipherText = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherText, tag);

        // Pack: nonce + tag + ciphertext
        var packed = new byte[NonceSize + TagSize + cipherText.Length];
        nonce.CopyTo(packed, 0);
        tag.CopyTo(packed, NonceSize);
        cipherText.CopyTo(packed, NonceSize + TagSize);

        return Prefix + Convert.ToBase64String(packed);
    }

    public string Unprotect(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return cipherText;

        // Backward compatible: if no ENC: prefix, return as-is (plaintext)
        if (!cipherText.StartsWith(Prefix, StringComparison.Ordinal))
            return cipherText;

        if (_key is null)
        {
            logger.LogWarning("Cannot decrypt connection string — no encryption key configured");
            return cipherText;
        }

        var packed = Convert.FromBase64String(cipherText[Prefix.Length..]);

        if (packed.Length < NonceSize + TagSize)
            throw new CryptographicException("Invalid encrypted connection string: data too short");

        var nonce = packed.AsSpan(0, NonceSize);
        var tag = packed.AsSpan(NonceSize, TagSize);
        var encrypted = packed.AsSpan(NonceSize + TagSize);

        var plainBytes = new byte[encrypted.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, encrypted, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[]? ResolveKey(IConfiguration configuration, IHostEnvironment environment)
    {
        var keyString = configuration["Security:ConnectionStringKey"];

        if (string.IsNullOrWhiteSpace(keyString))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException(
                    "Security:ConnectionStringKey is required in non-Development environments. " +
                    "Generate a 256-bit key: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))");

            return null; // Dev passthrough
        }

        var keyBytes = Convert.FromBase64String(keyString);
        if (keyBytes.Length != 32)
            throw new InvalidOperationException(
                "Security:ConnectionStringKey must be exactly 32 bytes (256 bits) when base64-decoded.");

        return keyBytes;
    }
}
