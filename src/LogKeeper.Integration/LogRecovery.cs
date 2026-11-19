using LogKeeper.Core;

namespace LogKeeper.Integration;

/// <summary>Відновлення журналу з наявних реплік і усунення дублікатів.</summary>
public static class LogRecovery
{
    /// <summary>Кожен пакет: PacketFormat.Read -> RecordCodec.ParseMany. Биті пакети
    /// (FormatException) пропускаються, решта записів дедуплікується.</summary>
    public static List<LogRecord> Recover(IReadOnlyList<byte[]> packets, string password)
        => Recover(packets, password, out _);

    /// <summary>Те саме, плюс кількість відкинутих (битих) пакетів.</summary>
    public static List<LogRecord> Recover(IReadOnlyList<byte[]> packets, string password, out int rejected)
    {
        ArgumentNullException.ThrowIfNull(packets);
        ArgumentNullException.ThrowIfNull(password);

        rejected = 0;
        var records = new List<LogRecord>();
        foreach (var packet in packets)
        {
            try
            {
                records.AddRange(RecordCodec.ParseMany(PacketFormat.Read(packet, password)));
            }
            catch (FormatException)
            {
                // битий пакет пропускаємо, записи візьмемо з інших реплік
                rejected++;
            }
        }
        return Deduplicate(records);
    }

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
