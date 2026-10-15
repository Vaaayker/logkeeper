namespace LogKeeper.Core;

/// <summary>Один запис журналу (16 байтів). Точні поля й типи — у FORMAT_SPEC_P3.md.</summary>
public sealed record LogRecord(
    long TimestampMs,
    int SourceId,
    int EventCode,
    int MetricValue,   // тип уточнити за FORMAT_SPEC_P3.md
    int Category,
    int Severity,
    int Critical,
    int RecordFlags);
