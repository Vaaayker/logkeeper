using LogKeeper.Core;

namespace LogKeeper.Integration.Data;

/// <summary>
/// Детермінований генератор журналу з мітками класів — запасне джерело даних, поки
/// в materials/ немає train.csv / valid.csv / session.bin (D-008).
/// Потік перемикається між режимами: 0 — штатна робота, 1 — аномалія (сплеск подій,
/// стрибки метрики, WARN/ERROR), 2 — перехід до інциденту (ERROR/FATAL, багато critical,
/// метрика росте). Мітка події — режим, у якому її згенеровано.
/// </summary>
public static class SyntheticLog
{
    public sealed record Labeled(List<LogRecord> Records, int[] Labels);

    public static Labeled Generate(int count, int seed, long startMs = 1_700_000_000_000)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

        var rng = new Random(seed);
        var records = new List<LogRecord>(count);
        var labels = new int[count];

        long t = startMs;
        int mode = 0, left = rng.Next(80, 200);
        double metric = 100;

        for (int i = 0; i < count; i++)
        {
            if (left-- <= 0)
            {
                // Після штатного режиму — аномалія (60%) або інцидент (40%); після них — знову штатний.
                mode = mode != 0 ? 0 : (rng.NextDouble() < 0.6 ? 1 : 2);
                left = mode == 0 ? rng.Next(80, 200) : rng.Next(25, 60);
            }

            byte severity, category;
            bool critical;
            switch (mode)
            {
                case 0:
                    t += rng.Next(300, 1500);
                    metric += rng.Next(-5, 6);
                    severity = (byte)(rng.NextDouble() < 0.85 ? rng.Next(0, 2) : 2);
                    category = (byte)rng.Next(0, 6);
                    critical = rng.NextDouble() < 0.01;
                    break;
                case 1:
                    t += rng.Next(40, 250);
                    metric += rng.Next(-300, 301);
                    severity = (byte)(rng.NextDouble() < 0.25 ? 1 : rng.Next(2, 4));
                    category = (byte)(rng.NextDouble() < 0.6 ? rng.Next(6, 8) : rng.Next(0, 6));
                    critical = rng.NextDouble() < 0.05;
                    break;
                default:
                    t += rng.Next(100, 600);
                    metric += rng.Next(10, 60);
                    severity = (byte)(rng.NextDouble() < 0.2 ? 2 : rng.Next(3, 5));
                    category = (byte)(rng.NextDouble() < 0.7 ? rng.Next(8, 10) : rng.Next(0, 8));
                    critical = rng.NextDouble() < 0.35;
                    break;
            }
            if (Math.Abs(metric) > 30_000) metric = 100;

            records.Add(new LogRecord(
                TimestampMs: t,
                SourceId: (ushort)rng.Next(1, 9),
                EventCode: (ushort)(1000 + category * 10 + rng.Next(0, 10)),
                MetricValue: (short)metric,
                Category: category,
                Severity: severity,
                Critical: critical,
                RecordFlags: 0));
            labels[i] = mode;
        }
        return new Labeled(records, labels);
    }
}
