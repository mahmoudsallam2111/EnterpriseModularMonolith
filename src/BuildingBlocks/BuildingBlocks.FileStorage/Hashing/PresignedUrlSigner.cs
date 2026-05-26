using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.FileStorage.Abstractions;

namespace BuildingBlocks.FileStorage.Hashing;

/// <summary>
/// HMAC-SHA256 signer for the local presigned URL scheme.
/// Signature payload: "{purpose}\n{container}\n{objectKey}\n{expiresUnix}".
/// </summary>
public sealed class PresignedUrlSigner
{
    private readonly byte[] _key;

    public PresignedUrlSigner(string signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
            throw new InvalidOperationException(
                "FileStorage:SigningKey must be set for Provider=Local. Generate a 32+ char secret.");
        _key = Encoding.UTF8.GetBytes(signingKey);
    }

    public string Sign(PresignedUrlPurpose purpose, string container, string objectKey, long expiresUnixSeconds)
    {
        var payload = $"{(int)purpose}\n{container}\n{objectKey}\n{expiresUnixSeconds}";
        using var hmac = new HMACSHA256(_key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool Verify(PresignedUrlPurpose purpose, string container, string objectKey, long expiresUnixSeconds, string signature)
    {
        if (DateTimeOffset.FromUnixTimeSeconds(expiresUnixSeconds) < DateTimeOffset.UtcNow) return false;
        var expected = Sign(purpose, container, objectKey, expiresUnixSeconds);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(signature));
    }
}
