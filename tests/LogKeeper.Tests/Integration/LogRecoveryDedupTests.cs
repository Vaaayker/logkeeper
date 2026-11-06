using LogKeeper.Compression;
using LogKeeper.Core;
using LogKeeper.Integration;

namespace LogKeeper.Tests.Integration;

public class LogRecoveryDedupTests
{
    private static readonly LogRecord A = new(1000, 1, 10, 5, 1, 1, false, 0);
    private static readonly LogRecord B = new(2000, 2, 20, -5, 2, 2, true, 1);
    private static readonly LogRecord C = new(3000, 3, 30, 0, 3, 4, false, 0xF);

    [Fact]
    public void Deduplicate_RemovesDuplicates_SortedByTime()
    {
        Assert.Equal([A, B, C], LogRecovery.Deduplicate([A, B, A, C, B]));
    }

    [Fact]
    public void Deduplicate_UnsortedInput_SortsByTimeThenSourceId()
    {
        var sameTimeSrc9 = new LogRecord(2000, 9, 1, 0, 0, 0, false, 0);
        var sameTimeSrc2 = new LogRecord(2000, 2, 1, 0, 0, 0, false, 0);

        var result = LogRecovery.Deduplicate([C, sameTimeSrc9, A, sameTimeSrc2, C]);

        Assert.Equal([A, sameTimeSrc2, sameTimeSrc9, C], result);
    }

    [Fact]
    public void Deduplicate_KeepsRecordsDifferingInOneField()
    {
        var aCritical = A with { Critical = true };

        Assert.Equal(2, LogRecovery.Deduplicate([A, aCritical, A]).Count);
    }

    [Fact]
    public void Deduplicate_Empty_ReturnsEmpty()
    {
        Assert.Empty(LogRecovery.Deduplicate([]));
    }

    [Fact]
    public void EndToEnd_SerializeMany_Huffman_ParseMany_ReturnsSameRecords()
    {
        var records = Enumerable.Range(0, 50)
            .Select(i => new LogRecord(
                TimestampMs: 1_761_134_400_000 + i * 250,
                SourceId: (ushort)(1 + i % 4),
                EventCode: (ushort)(100 + i % 3),
                MetricValue: (short)(i * 7 - 100),
                Category: (byte)(i % 10),
                Severity: (byte)(i % 5),
                Critical: i % 6 == 0,
                RecordFlags: (byte)(i % 16)))
            .ToList();

        var raw = RecordCodec.SerializeMany(records);
        var encoded = Huffman.Encode(raw);
        Assert.True(encoded.Length < raw.Length, "дані мають реально стиснутися");

        var restored = RecordCodec.ParseMany(Huffman.Decode(encoded));

        Assert.Equal(records, restored);
    }
}
