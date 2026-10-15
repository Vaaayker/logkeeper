namespace LogKeeper.Compression;

/// <summary>Канонічний Гаффман. Правила формату — FORMAT_SPEC_P3.md.</summary>
public static class Huffman
{
    /// <summary>Довжини кодів Гаффмана: {байт: довжина коду}.</summary>
    public static Dictionary<byte, int> CodeLengths(byte[] data) => throw new NotImplementedException();

    /// <summary>Канонічні коди тільки з довжин.</summary>
    public static Dictionary<byte, string> CanonicalCodes(IReadOnlyDictionary<byte, int> lengths)
        => throw new NotImplementedException();

    public static byte[] Encode(byte[] data) => throw new NotImplementedException();

    public static byte[] Decode(byte[] blob) => throw new NotImplementedException();
}
