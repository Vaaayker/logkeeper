using System.Buffers.Binary;

namespace LogKeeper.Core;

public static class RecordCodec
{
    public const int Size = 16;

    public const byte MaxCategory = 9;
    public const byte MaxSeverity = 4;
    public const byte FlagsMask = 0x0F;

    /// <summary>LogRecord -> рівно 16 байтів.</summary>
    public static byte[] Serialize(LogRecord record)
    {
        Validate(record);

        var data = new byte[Size];
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(0, 8), record.TimestampMs);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(8, 2), record.SourceId);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(10, 2), record.EventCode);
        BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(12, 2), record.MetricValue);
        data[14] = (byte)(record.Category | (record.Severity << 4) | (record.Critical ? 0x80 : 0));
        data[15] = record.RecordFlags;
        return data;
    }

    /// <summary>16 байтів -> LogRecord. Некоректні дані -> FormatException.</summary>
    public static LogRecord Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
            throw new FormatException($"Запис має бути {Size} байтів, отримано {data.Length}.");

        byte packed = data[14];
        var record = new LogRecord(
            TimestampMs: BinaryPrimitives.ReadInt64LittleEndian(data[..8]),
            SourceId: BinaryPrimitives.ReadUInt16LittleEndian(data[8..10]),
            EventCode: BinaryPrimitives.ReadUInt16LittleEndian(data[10..12]),
            MetricValue: BinaryPrimitives.ReadInt16LittleEndian(data[12..14]),
            Category: (byte)(packed & 0x0F),
            Severity: (byte)((packed >> 4) & 0x07),
            Critical: (packed & 0x80) != 0,
            RecordFlags: data[15]);

        Validate(record);
        return record;
    }

    private static void Validate(LogRecord r)
    {
        if (r.TimestampMs < 0)
            throw new FormatException($"TimestampMs не може бути від'ємним: {r.TimestampMs}.");
        if (r.SourceId == 0)
            throw new FormatException("SourceId = 0 заборонено.");
        if (r.Category > MaxCategory)
            throw new FormatException($"Category поза діапазоном 0..{MaxCategory}: {r.Category}.");
        if (r.Severity > MaxSeverity)
            throw new FormatException($"Severity поза діапазоном 0..{MaxSeverity}: {r.Severity}.");
        if ((r.RecordFlags & ~FlagsMask) != 0)
            throw new FormatException($"RecordFlags має встановлені зарезервовані біти: 0x{r.RecordFlags:X2}.");
    }
}
