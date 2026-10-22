namespace LogKeeper.Core;

/// <summary>Один запис журналу (16 байтів). Розкладка — у docs/FORMAT_SPEC_P3.md.</summary>
public sealed record LogRecord(
    long TimestampMs,
    ushort SourceId,
    ushort EventCode,
    short MetricValue,
    byte Category,
    byte Severity,
    bool Critical,
    byte RecordFlags);
