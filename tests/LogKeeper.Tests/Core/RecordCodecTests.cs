using LogKeeper.Core;

namespace LogKeeper.Tests.Core;

public class RecordCodecTests
{
    private static readonly LogRecord Sample = new(
        TimestampMs: 1_761_134_400_123,
        SourceId: 42,
        EventCode: 0xBEEF,
        MetricValue: -1234,
        Category: 7,
        Severity: 3,
        Critical: true,
        RecordFlags: 0b0101);

    [Fact]
    public void RoundTrip_ReturnsEqualRecord()
    {
        Assert.Equal(Sample, RecordCodec.Parse(RecordCodec.Serialize(Sample)));
    }

    [Fact]
    public void RoundTrip_BoundaryValues()
    {
        var max = new LogRecord(long.MaxValue, ushort.MaxValue, ushort.MaxValue, short.MinValue,
            RecordCodec.MaxCategory, RecordCodec.MaxSeverity, false, RecordCodec.FlagsMask);

        Assert.Equal(max, RecordCodec.Parse(RecordCodec.Serialize(max)));
    }

    [Fact]
    public void Serialize_ReturnsExactly16Bytes()
    {
        Assert.Equal(16, RecordCodec.Serialize(Sample).Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    public void Parse_WrongLength_Throws(int length)
    {
        Assert.Throws<FormatException>(() => RecordCodec.Parse(new byte[length]));
    }

    [Fact]
    public void Parse_CategoryOutOfRange_Throws()
    {
        var data = RecordCodec.Serialize(Sample);
        data[14] = (byte)((data[14] & 0xF0) | 10);

        Assert.Throws<FormatException>(() => RecordCodec.Parse(data));
    }

    [Fact]
    public void Parse_ReservedFlagBitsSet_Throws()
    {
        var data = RecordCodec.Serialize(Sample);
        data[15] |= 0x10;

        Assert.Throws<FormatException>(() => RecordCodec.Parse(data));
    }
}
