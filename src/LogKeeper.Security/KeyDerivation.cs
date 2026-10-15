namespace LogKeeper.Security;

/// <summary>PBKDF2-HMAC-SHA256: 100000 ітерацій, сіль 16 байтів, dkLen=64.</summary>
public static class KeyDerivation
{
    /// <summary>Повертає (KEnc, KMac): master[0..32] і master[32..64].</summary>
    public static (byte[] KEnc, byte[] KMac) DeriveKeys(string password, byte[] salt)
        => throw new NotImplementedException();
}
