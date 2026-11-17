using System;

namespace LogKeeper.Compression;

public static class CompressionStats
{
    public static (int OriginalBytes, int CompressedBytes, double Ratio) Measure(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return (0, 0, 1.0);
        }

        byte[] compressed = Huffman.Encode(data);
        int originalBytes = data.Length;
        int compressedBytes = compressed.Length;
        double ratio = (double)compressedBytes / originalBytes;

        return (originalBytes, compressedBytes, ratio);
    }
}
