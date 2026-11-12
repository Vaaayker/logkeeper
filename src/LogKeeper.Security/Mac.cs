using System.Security.Cryptography;

namespace LogKeeper.Security;

/// <summary>HMAC-SHA256 над заголовком і шифротекстом.</summary>
public static class Mac
{
    public const int TagSize = 32;

    /// <summary>HMAC-SHA256(key, header || ciphertext).</summary>
    public static byte[] Compute(byte[] key, byte[] header, byte[] ciphertext)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(ciphertext);

        using var hmac = new HMACSHA256(key);
        hmac.TransformBlock(header, 0, header.Length, null, 0);
        hmac.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        return hmac.Hash!;
    }

    /// <summary>Порівнює тег за сталий час. Тег неправильної довжини -> false.</summary>
    public static bool Verify(byte[] key, byte[] header, byte[] ciphertext, byte[] tag)
    {
        if (tag is null || tag.Length != TagSize) return false;
        return CryptographicOperations.FixedTimeEquals(Compute(key, header, ciphertext), tag);
    }
}
