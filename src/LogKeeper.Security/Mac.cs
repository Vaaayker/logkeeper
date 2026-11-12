using System;
using System.Security.Cryptography;

namespace LogKeeper.Security;

/// <summary>HMAC-SHA256 над заголовком і шифротекстом.</summary>
public static class Mac
{
    public const int TagSize = 32;

    /// <summary>Обчислює HMAC-SHA256(key, header || ciphertext).</summary>
    public static byte[] Compute(byte[] key, byte[] header, byte[] ciphertext)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(ciphertext);

        using var hmac = IncrementalHash.CreateHMAC(
            HashAlgorithmName.SHA256, key);

        hmac.AppendData(header);
        hmac.AppendData(ciphertext);

        return hmac.GetHashAndReset();
    }

    /// <summary>Порівнює тег за сталий час. Тег неправильної довжини повертає false.</summary>
    public static bool Verify(
        byte[] key, byte[] header, byte[] ciphertext, byte[] tag)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(ciphertext);
        ArgumentNullException.ThrowIfNull(tag);

        if (tag.Length != TagSize)
            return false;

        byte[] expectedTag = Compute(key, header, ciphertext);

        return CryptographicOperations.FixedTimeEquals(expectedTag, tag);
    }
}
