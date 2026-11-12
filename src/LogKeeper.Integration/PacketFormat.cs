using System.Buffers.Binary;
using System.Security.Cryptography;
using LogKeeper.Compression;
using LogKeeper.Security;

namespace LogKeeper.Integration;

/// <summary>Формат репліки LOG3: відкритий заголовок + шифроване навантаження + HMAC.
/// Розкладка — у docs/FORMAT_SPEC_P3.md, розділ "Репліка LOG3".</summary>
public static class PacketFormat
{
    public const byte Version = 1;
    public const int SaltSize = 16;
    public const int NonceSize = AesCtr.NonceSize;
    public const int TagSize = Mac.TagSize;

    private static readonly byte[] Magic = "LOG3"u8.ToArray();

    // magic(4) | version(1) | salt(16) | nonce(12) | len(4)
    public const int HeaderSize = 4 + 1 + SaltSize + NonceSize + 4;
    private const int SaltOffset = 5;
    private const int NonceOffset = SaltOffset + SaltSize;
    private const int LenOffset = NonceOffset + NonceSize;

    /// <summary>Стиснення (Huffman) -> AES-256-CTR -> HMAC-SHA256. Сіль і nonce випадкові для кожного пакета.</summary>
    public static byte[] Build(byte[] payload, string password)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var (kEnc, kMac) = KeyDerivation.DeriveKeys(password, salt);

        byte[] ciphertext = AesCtr.Crypt(kEnc, nonce, Huffman.Encode(payload));

        var packet = new byte[HeaderSize + ciphertext.Length + TagSize];
        Magic.CopyTo(packet, 0);
        packet[4] = Version;
        salt.CopyTo(packet, SaltOffset);
        nonce.CopyTo(packet, NonceOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(LenOffset, 4), (uint)ciphertext.Length);
        ciphertext.CopyTo(packet, HeaderSize);

        byte[] header = packet[..HeaderSize];
        Mac.Compute(kMac, header, ciphertext).CopyTo(packet, HeaderSize + ciphertext.Length);
        return packet;
    }

    /// <summary>Перевіряє magic, довжину, HMAC; при помилці — FormatException (без аварії програми).</summary>
    public static byte[] Read(byte[] data, string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        if (data is null || data.Length < HeaderSize + TagSize)
            throw new FormatException("Пакет LOG3 занадто короткий.");
        if (!data.AsSpan(0, 4).SequenceEqual(Magic))
            throw new FormatException("Неправильний magic, очікувався LOG3.");
        if (data[4] != Version)
            throw new FormatException($"Непідтримувана версія LOG3: {data[4]}.");

        uint len = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(LenOffset, 4));
        if ((long)HeaderSize + len + TagSize != data.Length)
            throw new FormatException("Довжина пакета не збігається з полем len.");

        byte[] header = data[..HeaderSize];
        byte[] salt = data[SaltOffset..NonceOffset];
        byte[] nonce = data[NonceOffset..LenOffset];
        byte[] ciphertext = data[HeaderSize..(HeaderSize + (int)len)];
        byte[] tag = data[(HeaderSize + (int)len)..];

        var (kEnc, kMac) = KeyDerivation.DeriveKeys(password, salt);
        if (!Mac.Verify(kMac, header, ciphertext, tag))
            throw new FormatException("HMAC не збігається: неправильний пароль або пакет пошкоджено.");

        byte[] compressed = AesCtr.Crypt(kEnc, nonce, ciphertext);
        try
        {
            return Huffman.Decode(compressed);
        }
        catch (Exception ex) when (ex is not FormatException)
        {
            throw new FormatException("Не вдалося розпакувати навантаження.", ex);
        }
    }
}
