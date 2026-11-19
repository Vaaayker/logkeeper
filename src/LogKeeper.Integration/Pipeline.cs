using System.Diagnostics;
using LogKeeper.Compression;
using LogKeeper.Core;
using LogKeeper.Integration.Replication;
using LogKeeper.Scheduler;

namespace LogKeeper.Integration;

/// <summary>
/// Результат наскрізного запуску (визначення метрик — D-007):
/// S — частка відновлених критичних подій (Critical = 1; якщо їх немає — подій класу 2);
/// R — частка відновлених подій журналу;
/// T — ощадливість сховища: 1 − Σ w_i·r_i / (3 · Σ w_i);
/// Q = α·S + β·R + γ·T.
/// </summary>
public sealed record PipelineResult(double S, double R, double T, double Q)
{
    public string Scenario { get; init; } = "";
    public int TotalEvents { get; init; }
    public int RecoveredEvents { get; init; }
    public int CriticalEvents { get; init; }
    public int CriticalRecovered { get; init; }
    /// <summary>Записи, яких не було у вхідному журналі (мають бути 0 — інакше HMAC пропустив сміття).</summary>
    public int SpuriousRecords { get; init; }
    public int PacketsWritten { get; init; }
    public int PacketsRead { get; init; }
    public int PacketsRejected { get; init; }
    public long BytesRaw { get; init; }
    public long BytesCompressed { get; init; }
    public long BytesWritten { get; init; }
    public long PlanUtility { get; init; }
    public int[] Copies { get; init; } = Array.Empty<int>();
    public int[] Classes { get; init; } = Array.Empty<int>();
    public double ElapsedMs { get; init; }
}

/// <summary>Повний шлях даних: події -> клас -> стиснення -> копії -> порядок -> шифр -> вузли -> відновлення.</summary>
public static class Pipeline
{
    public static PipelineResult Run(IReadOnlyList<LogRecord> records, string scenario)
        => Run(records, scenario, PipelineOptions.Default);

    public static PipelineResult Run(IReadOnlyList<LogRecord> records, string scenario, PipelineOptions options)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(options);
        var config = options.Config;
        var failure = FailureScenario.Parse(scenario, config.Nodes);
        var sw = Stopwatch.StartNew();
        int n = records.Count;

        // 1. Класифікація.
        int[] classes = options.Classifier?.Predict(records) ?? CopyPlanning.RuleBasedClasses(records);

        // 2. Ваги за Гаффманом, корисності, бюджет -> 3. кількість копій (ДП або жадібний).
        var parameters = options.EffectiveParams;
        var problem = CopyPlanning.BuildProblem(records, classes, parameters, config.NodeFailureProbability,
            options.DpCellLimit);
        int[] copies = CopyPlanning.Choose(problem, options.Strategy);
        int maxCopies = Math.Min(Knapsack.MaxCopies, config.Nodes.Count);
        for (int i = 0; i < n; i++) copies[i] = Math.Min(copies[i], maxCopies);

        // 4. Порядок: найцінніші події першими (вони потрапляють у перші пакети кожного вузла),
        //    далі розподіл копій по вузлах round-robin (Placement гарантує різні вузли для копій).
        int[] order = Enumerable.Range(0, n)
            .OrderByDescending(i => problem.Values[i])
            .ThenBy(i => records[i].TimestampMs)
            .ThenBy(i => i)
            .ToArray();
        int[] orderedCopies = order.Select(i => copies[i]).ToArray();
        var batches = Placement.PlanBatches(orderedCopies, config.Nodes);

        // 5. Захист і запис: SerializeMany -> Huffman -> PBKDF2 -> AES-CTR -> HMAC (PacketFormat.Build).
        var store = options.StoreFactory();
        int packetsWritten = 0;
        long bytesWritten = 0;
        try
        {
            foreach (var node in config.Nodes)
            {
                var positions = batches.TryGetValue(node, out var b) ? b : new List<int>();
                var packets = new List<byte[]>();
                for (int start = 0; start < positions.Count; start += options.PacketRecords)
                {
                    var chunk = positions.Skip(start).Take(options.PacketRecords)
                        .Select(pos => records[order[pos]]).ToList();
                    packets.Add(PacketFormat.Build(RecordCodec.SerializeMany(chunk), options.Password));
                }
                store.Write(node, packets);
                packetsWritten += packets.Count;
                bytesWritten += packets.Sum(p => (long)p.Length);
            }

            // 6. Відмови вузлів за сценарієм.
            store.Apply(failure);

            // 7. Відновлення: HMAC -> розшифрування -> Huffman.Decode -> ParseMany -> дедуплікація.
            var survived = store.ReadAll();
            var recovered = LogRecovery.Recover(survived, options.Password, out int rejected);

            // 8. Метрики.
            var original = new HashSet<LogRecord>(records);
            var recoveredSet = new HashSet<LogRecord>(recovered);
            int recoveredCount = original.Count(recoveredSet.Contains);
            int spurious = recoveredSet.Count(r => !original.Contains(r));

            var important = Enumerable.Range(0, n).Where(i => records[i].Critical).ToList();
            if (important.Count == 0)
                important = Enumerable.Range(0, n).Where(i => classes[i] == 2).ToList();
            var importantSet = important.Select(i => records[i]).ToHashSet();
            int importantRecovered = importantSet.Count(recoveredSet.Contains);

            double s = importantSet.Count == 0 ? 1 : (double)importantRecovered / importantSet.Count;
            double r = original.Count == 0 ? 1 : (double)recoveredCount / original.Count;
            long oneCopy = problem.Weights.Sum(w => (long)w);
            double t = oneCopy == 0 ? 1 : 1 - (double)problem.Weight(copies) / (Knapsack.MaxCopies * oneCopy);
            double q = QualityMetrics.ComputeQ(s, r, t, config.Alpha, config.Beta, config.Gamma);

            byte[] raw = RecordCodec.SerializeMany(records);
            return new PipelineResult(s, r, t, q)
            {
                Scenario = failure.Name,
                TotalEvents = original.Count,
                RecoveredEvents = recoveredCount,
                CriticalEvents = importantSet.Count,
                CriticalRecovered = importantRecovered,
                SpuriousRecords = spurious,
                PacketsWritten = packetsWritten,
                PacketsRead = survived.Count,
                PacketsRejected = rejected,
                BytesRaw = raw.Length,
                BytesCompressed = Huffman.Encode(raw).Length,
                BytesWritten = bytesWritten,
                PlanUtility = problem.Utility(copies),
                Copies = copies,
                Classes = classes,
                ElapsedMs = sw.Elapsed.TotalMilliseconds,
            };
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }
}
