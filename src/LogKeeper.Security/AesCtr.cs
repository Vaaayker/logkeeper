using System.Buffers.Binary;
using System.Security.Cryptography;

namespace LogKeeper.Security;

/// <summary>AES-256-CTR, лічильник = nonce(12) || uint32_BE(i), i починається з 1.</summary>
public static class AesCtr
{
    public const int KeySize = 32;
    public const int NonceSize = 12;
    private const int BlockSize = 16;

    /// <summary>Шифрує або розшифровує (у CTR це одна й та сама операція).</summary>
    public static byte[] Crypt(byte[] key, byte[] nonce, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(nonce);
        ArgumentNullException.ThrowIfNull(data);
        if (key.Length != KeySize)
            throw new ArgumentException($"Ключ має бути {KeySize} байти.", nameof(key));
        if (nonce.Length != NonceSize)
            throw new ArgumentException($"Nonce має бути {NonceSize} байтів.", nameof(nonce));

        int blocks = (data.Length + BlockSize - 1) / BlockSize;
        if ((ulong)blocks > uint.MaxValue)
            throw new ArgumentException("Дані задовгі для 32-бітного лічильника.", nameof(data));

        // Усі блоки лічильника одразу, потім один виклик AES-ECB дає потік ключа.
        var counters = new byte[blocks * BlockSize];
        for (int i = 0; i < blocks; i++)
        {
            var block = counters.AsSpan(i * BlockSize, BlockSize);
            nonce.CopyTo(block);
            BinaryPrimitives.WriteUInt32BigEndian(block[NonceSize..], (uint)(i + 1));
        }

        using var aes = Aes.Create();
        aes.Key = key;
        byte[] keystream = aes.EncryptEcb(counters, PaddingMode.None);

        var result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
            result[i] = (byte)(data[i] ^ keystream[i]);
        return result;
    }
}
