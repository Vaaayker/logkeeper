using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LogKeeper.Compression;
using LogKeeper.Core;

namespace LogKeeper.Tests.Compression;

public class HuffmanTests
{
    // aabc -> a:1, b:2, c:2
    [Fact]
    public void CodeLengths_Standard_ReturnsExpectedLengths()
    {
        byte[] data = Encoding.UTF8.GetBytes("aabc");
        var lengths = Huffman.CodeLengths(data);

        Assert.Equal(3, lengths.Count);
        Assert.Equal(1, lengths[(byte)'a']);
        Assert.Equal(2, lengths[(byte)'b']);
        Assert.Equal(2, lengths[(byte)'c']);
    }

    // порожній вхід -> порожній словник
    [Fact]
    public void CodeLengths_EmptyInput_ReturnsEmptyDictionary()
    {
        byte[] empty = new byte[0];
        var lengths = Huffman.CodeLengths(empty);
        Assert.Empty(lengths);
    }

    // один символ -> довжина 1
    [Fact]
    public void CodeLengths_SingleSymbol_ReturnsCodeLengthOne()
    {
        byte[] data = Encoding.UTF8.GetBytes("aaaaa");
        var lengths = Huffman.CodeLengths(data);

        Assert.Single(lengths);
        Assert.Equal(1, lengths[(byte)'a']);
    }

    // канонічні коди: a:1, b:2, c:2 -> a="0", b="10", c="11"
    [Fact]
    public void CanonicalCodes_ReturnsExpectedPrefixCodes()
    {
        var lengths = new Dictionary<byte, int>();
        lengths[(byte)'a'] = 1;
        lengths[(byte)'b'] = 2;
        lengths[(byte)'c'] = 2;

        var codes = Huffman.CanonicalCodes(lengths);

        Assert.Equal("0", codes[(byte)'a']);
        Assert.Equal("10", codes[(byte)'b']);
        Assert.Equal("11", codes[(byte)'c']);
    }

    // round-trip для порожнього масиву
    [Fact]
    public void RoundTrip_EmptyArray_RestoresCorrectly()
    {
        byte[] empty = new byte[0];
        byte[] encoded = Huffman.Encode(empty);
        byte[] decoded = Huffman.Decode(encoded);

        Assert.Equal(empty, decoded);
    }

    // round-trip для одного байта
    [Fact]
    public void RoundTrip_SingleByte_RestoresCorrectly()
    {
        byte[] single = new byte[] { 42 };
        byte[] encoded = Huffman.Encode(single);
        byte[] decoded = Huffman.Decode(encoded);

        Assert.Equal(single, decoded);
    }

    // round-trip для 1000 однакових байтів
    [Fact]
    public void RoundTrip_ThousandIdenticalBytes_RestoresCorrectly()
    {
        byte[] identical = new byte[1000];
        for (int i = 0; i < 1000; i++)
        {
            identical[i] = (byte)'X';
        }

        byte[] encoded = Huffman.Encode(identical);
        byte[] decoded = Huffman.Decode(encoded);

        Assert.Equal(identical, decoded);
    }

    // round-trip для 1000 рандомних байтів
    [Fact]
    public void RoundTrip_ThousandRandomBytes_RestoresCorrectly()
    {
        var rnd = new Random(42);
        byte[] randomBytes = new byte[1000];
        rnd.NextBytes(randomBytes);

        byte[] encoded = Huffman.Encode(randomBytes);
        byte[] decoded = Huffman.Decode(encoded);

        Assert.Equal(randomBytes, decoded);
    }

    // round-trip для 100 записів через RecordCodec.Serialize
    [Fact]
    public void RoundTrip_HundredSerializedRecords_RestoresCorrectly()
    {
        using var stream = new MemoryStream();
        for (int i = 0; i < 100; i++)
        {
            var record = new LogRecord(
                TimestampMs: 1_700_000_000_000 + i,
                SourceId: (ushort)(1 + (i % 5)),
                EventCode: (ushort)(100 + i),
                MetricValue: (short)(i * 10),
                Category: (byte)(i % 10),
                Severity: (byte)(i % 5),
                Critical: (i % 2 == 0),
                RecordFlags: 0);

            byte[] serialized = RecordCodec.Serialize(record);
            stream.Write(serialized, 0, serialized.Length);
        }

        byte[] allRecordsBytes = stream.ToArray();
        Assert.Equal(1600, allRecordsBytes.Length);

        byte[] encoded = Huffman.Encode(allRecordsBytes);
        byte[] decoded = Huffman.Decode(encoded);

        Assert.Equal(allRecordsBytes, decoded);
    }

    // якщо не стискається (32 випадкових байти) -> перший байт 0
    [Fact]
    public void Encode_UncompressibleData_UsesNoCompressionMode()
    {
        var rnd = new Random(42);
        byte[] uncompressible = new byte[32];
        rnd.NextBytes(uncompressible);

        byte[] encoded = Huffman.Encode(uncompressible);

        // перший байт 0 -> MODE_NO_COMPRESSION
        Assert.Equal(0, encoded[0]);

        byte[] decoded = Huffman.Decode(encoded);
        Assert.Equal(uncompressible, decoded);
    }

    // невідомий mode = 7 -> FormatException
    [Fact]
    public void Decode_InvalidMode_ThrowsFormatException()
    {
        byte[] invalidBlob = new byte[] { 7, 0, 0, 0, 0 };
        Assert.Throws<FormatException>(() => Huffman.Decode(invalidBlob));
    }

    // обрізаний навпіл блоб -> FormatException
    [Fact]
    public void Decode_TruncatedBlobInHalf_ThrowsFormatException()
    {
        byte[] text = Encoding.UTF8.GetBytes("aaaaabbbbbcccccdddddeeeeefffffggggghhhhhiiiiijjjjj");
        byte[] encoded = Huffman.Encode(text);

        int halfLength = encoded.Length / 2;
        byte[] halfBlob = new byte[halfLength];
        Array.Copy(encoded, 0, halfBlob, 0, halfLength);

        Assert.Throws<FormatException>(() => Huffman.Decode(halfBlob));
    }
}
