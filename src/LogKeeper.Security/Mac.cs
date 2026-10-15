namespace LogKeeper.Security;

/// <summary>HMAC-SHA256 над заголовком і шифротекстом.</summary>
public static class Mac
{
    public static byte[] Compute(byte[] key, byte[] header, byte[] ciphertext)
        => throw new NotImplementedException();

    public static bool Verify(byte[] key, byte[] header, byte[] ciphertext, byte[] tag)
        => throw new NotImplementedException();
}
