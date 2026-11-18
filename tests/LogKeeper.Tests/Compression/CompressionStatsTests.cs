using System;
using System.Collections.Generic;
using System.Linq;
using LogKeeper.Compression;
using LogKeeper.Core;

namespace LogKeeper.Tests.Compression;

public class CompressionStatsTests
{
    [Fact]
    public void Measure_RandomBytes_TriggersNoCompression_NotExceedingPlusOneByte()
    {
        var rnd = new Random(42);
        byte[] randomBytes = new byte[1000];
        rnd.NextBytes(randomBytes);

        var (originalBytes, compressedBytes, ratio) = CompressionStats.Measure(randomBytes);

        Assert.Equal(1000, originalBytes);
        Assert.True(compressedBytes <= originalBytes + 1);
        Assert.Equal(randomBytes, Huffman.Decode(Huffman.Encode(randomBytes)));
    }

    [Fact]
    public void Measure_IdenticalRecords_RatioLessThanOne()
    {
        var record = new LogRecord(
            TimestampMs: 1_700_000_000_000,
            SourceId: 1,
            EventCode: 100,
            MetricValue: 42,
            Category: 2,
            Severity: 1,
            Critical: false,
            RecordFlags: 0);

        var records = Enumerable.Repeat(record, 100).ToList();
        byte[] data = RecordCodec.SerializeMany(records);

        var (originalBytes, compressedBytes, ratio) = CompressionStats.Measure(data);

        Assert.Equal(1600, originalBytes);
        Assert.True(ratio < 1.0);
        Assert.True(compressedBytes < originalBytes);
        Assert.Equal(data, Huffman.Decode(Huffman.Encode(data)));
    }

    [Fact]
    public void Measure_ThousandGeneratedRecords_RestoresCorrectly()
    {
        var records = new List<LogRecord>();
        for (int i = 0; i < 1000; i++)
        {
            records.Add(new LogRecord(
                TimestampMs: 1_700_000_000_000 + i * 50,
                SourceId: (ushort)(1 + (i % 8)),
                EventCode: (ushort)(200 + (i % 15)),
                MetricValue: (short)(i % 50),
                Category: (byte)(i % 5),
                Severity: (byte)(i % 4),
                Critical: (i % 10 == 0),
                RecordFlags: (byte)(i % 2)));
        }

        byte[] data = RecordCodec.SerializeMany(records);
        var (originalBytes, compressedBytes, ratio) = CompressionStats.Measure(data);

        Assert.Equal(16000, originalBytes);
        Assert.True(ratio < 1.0);
        Assert.Equal(data, Huffman.Decode(Huffman.Encode(data)));
    }
}
