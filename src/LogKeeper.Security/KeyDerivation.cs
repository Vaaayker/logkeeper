namespace LogKeeper.Security;

using System;
using System.Security.Cryptography;

/// <summary>
/// PBKDF2-HMAC-SHA256: 100000 ітерацій, сіль 16 байтів, dkLen=64.
/// </summary>
public static class KeyDerivation
{
    /// <summary>
    /// Повертає два ключі: KEnc (перші 32 байти)
    /// і KMac (наступні 32 байти).
    /// </summary>
    public static (byte[] KEnc, byte[] KMac) DeriveKeys(
        string password,
        byte[] salt)
    {
        if (password == null)
            throw new ArgumentNullException(nameof(password));

        if (salt == null || salt.Length != 16)
            throw new ArgumentException(
                "Salt must be exactly 16 bytes.",
                nameof(salt));

        byte[] master = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            64);

        byte[] kEnc = new byte[32];
        byte[] kMac = new byte[32];

        Array.Copy(master, 0, kEnc, 0, 32);
        Array.Copy(master, 32, kMac, 0, 32);

        CryptographicOperations.ZeroMemory(master);

        return (kEnc, kMac);
    }
}
