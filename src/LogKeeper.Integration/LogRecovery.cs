using LogKeeper.Core;

namespace LogKeeper.Integration;

/// <summary>Відновлення журналу з наявних реплік і усунення дублікатів.</summary>
public static class LogRecovery
{
    public static List<LogRecord> Recover(IReadOnlyList<byte[]> packets, string password)
        => throw new NotImplementedException();

    /// <summary>Прибирає повністю однакові записи (порівняння всіх полів) і сортує
    /// за TimestampMs, а при однаковому часі — за SourceId.</summary>
    public static List<LogRecord> Deduplicate(IEnumerable<LogRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        return records
            .Distinct()
            .OrderBy(r => r.TimestampMs)
            .ThenBy(r => r.SourceId)
            .ToList();
    }
}
