using LogKeeper.Compression;
using LogKeeper.Core;
using LogKeeper.Scheduler;

namespace LogKeeper.Integration;

/// <summary>Параметри цінності подій і бюджету — їх налаштовує ГА (D-006).</summary>
public sealed record SchedulerParams(
    double ValueNormal = 1,
    double ValueAnomaly = 4,
    double ValuePreIncident = 10,
    double CriticalBonus = 10,
    double BudgetFactor = 1.5)
{
    public static SchedulerParams Default { get; } = new();

    public double ValueOf(int eventClass, bool critical) =>
        (eventClass switch { 2 => ValuePreIncident, 1 => ValueAnomaly, _ => ValueNormal })
        + (critical ? CriticalBonus : 0);
}

public enum CopyStrategy { DynamicProgramming, Greedy }

/// <summary>Задача для планувальника: ваги (байти після стиснення), корисності, бюджет.</summary>
public sealed record CopyProblem(int[] Weights, int[][] Utilities, int Budget, double[] Values, int ScaleDivisor)
{
    public long Utility(int[] copies) => copies.Select((r, i) => (long)Utilities[i][r]).Sum();
    public long Weight(int[] copies) => copies.Select((r, i) => (long)Weights[i] * r).Sum();
}

public static class CopyPlanning
{
    /// <summary>Множник для цілочисельних корисностей.</summary>
    public const int UtilityScale = 100;
    /// <summary>Межа розміру таблиці ДП (n × budget), щоб пам'ять лишалась у межах ~40 МБ.</summary>
    public const long DefaultDpCellLimit = 40_000_000;

    /// <summary>
    /// Вага події w_i — розмір її 16 байтів у бітах за кодами Гаффмана всієї сесії, округлений
    /// до байтів (≥ 1). Корисність u_i(r) = v_i · (1 − p^r) — ймовірність, що хоча б одна з r
    /// копій переживе відмови вузлів. Бюджет = BudgetFactor · Σ w_i.
    /// Якщо n · budget завеликий, ваги й бюджет ділимо на спільний дільник (ScaleDivisor).
    /// </summary>
    public static CopyProblem BuildProblem(IReadOnlyList<LogRecord> records, IReadOnlyList<int> classes,
        SchedulerParams p, double nodeFailureProbability, long dpCellLimit = DefaultDpCellLimit)
    {
        int n = records.Count;
        byte[] raw = RecordCodec.SerializeMany(records);
        var lengths = Huffman.CodeLengths(raw);

        var weights = new int[n];
        long total = 0;
        for (int i = 0; i < n; i++)
        {
            int bits = 0;
            for (int b = i * RecordCodec.Size; b < (i + 1) * RecordCodec.Size; b++)
                bits += lengths[raw[b]];
            weights[i] = Math.Max(1, (bits + 7) / 8);
            total += weights[i];
        }

        long budget = (long)Math.Floor(Math.Max(0, p.BudgetFactor) * total);
        int divisor = 1;
        if (n > 0 && (long)n * (budget + 1) > dpCellLimit)
        {
            divisor = (int)Math.Ceiling((double)n * (budget + 1) / dpCellLimit);
            for (int i = 0; i < n; i++) weights[i] = (weights[i] + divisor - 1) / divisor;
            budget /= divisor;
        }

        var values = new double[n];
        var utilities = new int[n][];
        double q = Math.Clamp(nodeFailureProbability, 0, 1);
        for (int i = 0; i < n; i++)
        {
            values[i] = Math.Max(0, p.ValueOf(classes[i], records[i].Critical));
            utilities[i] = new int[Knapsack.MaxCopies + 1];
            for (int r = 1; r <= Knapsack.MaxCopies; r++)
                utilities[i][r] = (int)Math.Round(UtilityScale * values[i] * (1 - Math.Pow(q, r)));
        }
        return new CopyProblem(weights, utilities, (int)Math.Min(budget, int.MaxValue), values, divisor);
    }

    public static int[] Choose(CopyProblem problem, CopyStrategy strategy) => strategy switch
    {
        CopyStrategy.DynamicProgramming => Knapsack.ChooseCopies(problem.Weights, problem.Utilities, problem.Budget),
        CopyStrategy.Greedy => GreedyScheduler.ChooseCopies(problem.Weights, problem.Utilities, problem.Budget),
        _ => throw new ArgumentOutOfRangeException(nameof(strategy)),
    };

    /// <summary>Клас без перцептрона (для абляції): за severity і прапорцем critical.</summary>
    public static int[] RuleBasedClasses(IReadOnlyList<LogRecord> records) =>
        records.Select(r => r.Critical || r.Severity >= 4 ? 2 : r.Severity == 3 ? 1 : 0).ToArray();
}
