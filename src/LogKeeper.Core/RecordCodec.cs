namespace LogKeeper.Core;

public static class RecordCodec
{
    public const int Size = 16;

    /// <summary>LogRecord -> рівно 16 байтів.</summary>
    public static byte[] Serialize(LogRecord record) => throw new NotImplementedException();

    /// <summary>16 байтів -> LogRecord. Некоректні дані -> FormatException.</summary>
    public static LogRecord Parse(ReadOnlySpan<byte> data) => throw new NotImplementedException();
}
