using System.Globalization;
using LogKeeper.Classifier;
using LogKeeper.Core;

namespace LogKeeper.Integration.Data;

/// <summary>
/// Читання session.csv / train.csv / valid.csv у <see cref="LogRecord"/> (+ мітка класу, якщо є).
/// Назви колонок у файлах викладача ще не звірені (D-003), тому підтримуються синоніми.
/// </summary>
public static class RecordCsv
{
    private static readonly string[] TimestampNames = { "timestamp_ms", "timestampms", "timestamp", "ts", "time_ms", "time" };
    private static readonly string[] SourceNames = { "source_id", "sourceid", "source", "src" };
    private static readonly string[] CodeNames = { "event_code", "eventcode", "code", "event" };
    private static readonly string[] MetricNames = { "metric_value", "metricvalue", "metric", "value" };
    private static readonly string[] CategoryNames = { "category", "cat" };
    private static readonly string[] SeverityNames = { "severity", "sev", "level" };
    private static readonly string[] CriticalNames = { "critical", "is_critical" };
    private static readonly string[] FlagsNames = { "record_flags", "recordflags", "flags" };
    private static readonly string[] LabelNames = { "class", "label", "y" };

    public sealed record Table(List<LogRecord> Records, int[]? Labels);

    public static Table Load(string path)
    {
        var rows = DatasetLoader.LoadEvents(path);
        if (rows.Count == 0) return new Table(new List<LogRecord>(), null);

        var columns = rows[0].Keys.ToList();
        string Col(string[] names, bool required = true)
        {
            var c = columns.FirstOrDefault(k => names.Contains(k.Trim().ToLowerInvariant()));
            if (c is null && required)
                throw new FormatException($"{Path.GetFileName(path)}: немає колонки ({string.Join("/", names)}).");
            return c ?? "";
        }

        string ts = Col(TimestampNames), src = Col(SourceNames), code = Col(CodeNames),
               metric = Col(MetricNames), cat = Col(CategoryNames), sev = Col(SeverityNames),
               crit = Col(CriticalNames), flags = Col(FlagsNames, required: false),
               label = Col(LabelNames, required: false);

        var records = new List<LogRecord>(rows.Count);
        var labels = label == "" ? null : new int[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            records.Add(new LogRecord(
                TimestampMs: (long)Num(r[ts]),
                SourceId: (ushort)Num(r[src]),
                EventCode: (ushort)Num(r[code]),
                MetricValue: (short)Math.Clamp(Math.Round(Num(r[metric])), short.MinValue, short.MaxValue),
                Category: (byte)Num(r[cat]),
                Severity: (byte)Num(r[sev]),
                Critical: Bool(r[crit]),
                RecordFlags: flags == "" ? (byte)0 : (byte)Num(r[flags])));
            if (labels is not null) labels[i] = (int)Num(r[label]);
        }
        return new Table(records, labels);
    }

    public static void Save(string path, IReadOnlyList<LogRecord> records, IReadOnlyList<int>? labels = null)
    {
        using var w = new StreamWriter(path);
        w.WriteLine("timestamp_ms,source_id,event_code,metric_value,category,severity,critical,record_flags"
                    + (labels is null ? "" : ",class"));
        for (int i = 0; i < records.Count; i++)
        {
            var r = records[i];
            w.Write(string.Join(",", r.TimestampMs, r.SourceId, r.EventCode, r.MetricValue,
                r.Category, r.Severity, r.Critical ? 1 : 0, r.RecordFlags));
            if (labels is not null) w.Write("," + labels[i]);
            w.WriteLine();
        }
    }

    private static double Num(string s) => double.Parse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);

    private static bool Bool(string s) => s.Trim().ToLowerInvariant() switch
    {
        "1" or "true" or "yes" => true,
        "0" or "false" or "no" or "" => false,
        var other => throw new FormatException($"Некоректне значення critical: '{other}'."),
    };
}
