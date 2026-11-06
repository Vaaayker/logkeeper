using LogKeeper.Core;

namespace LogKeeper.Tests.Core;

public class RecordCodecBatchTests
{
    private static readonly LogRecord[] Three =
    [
        new(1_761_134_400_123, 42, 0xBEEF, -1234, 7, 3, true, 0b0101),
        new(1_761_134_400_500, 7, 1, 0, 0, 0, false, 0),
        new(long.MaxValue, ushort.MaxValue, ushort.MaxValue, short.MinValue,
            RecordCodec.MaxCategory, RecordCodec.MaxSeverity, false, RecordCodec.FlagsMask),
    ];

    [Fact]
    public void RoundTrip_ThreeRecords()
    {
        Assert.Equal(Three, RecordCodec.ParseMany(RecordCodec.SerializeMany(Three)));
    }

    [Fact]
    public void RoundTrip_EmptyList()
    {
        var bytes = RecordCodec.SerializeMany([]);

        Assert.Empty(bytes);
        Assert.Empty(RecordCodec.ParseMany(bytes));
    }

    [Fact]
    public void SerializeMany_IsConcatenationOfSingleRecords()
    {
        var expected = Three.SelectMany(RecordCodec.Serialize).ToArray();

        Assert.Equal(expected, RecordCodec.SerializeMany(Three));
    }

    [Theory]
    [InlineData(17)]
    [InlineData(31)]
    public void ParseMany_LengthNotMultipleOf16_Throws(int length)
    {
        Assert.Throws<FormatException>(() => RecordCodec.ParseMany(new byte[length]));
    }

    [Fact]
    public void ParseMany_InvalidRecordInsideBatch_Throws()
    {
        var bytes = RecordCodec.SerializeMany(Three);
        bytes[RecordCodec.Size + 15] |= 0x10; // зарезервований біт у другому записі

        Assert.Throws<FormatException>(() => RecordCodec.ParseMany(bytes));
    }
}
