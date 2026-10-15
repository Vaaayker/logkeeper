namespace LogKeeper.Security;

/// <summary>AES-256-CTR, лічильник = nonce(12) || uint32_BE(i), i починається з 1.</summary>
public static class AesCtr
{
    public static byte[] Crypt(byte[] key, byte[] nonce, byte[] data) => throw new NotImplementedException();
}
