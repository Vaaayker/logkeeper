using LogKeeper.Core;

namespace LogKeeper.Classifier;

/// <summary>
/// Класифікатор подій на основі <see cref="Mlp"/>: для кожної події будується вікно
/// попередніх записів тривалістю <see cref="WindowMs"/> (FeatureBuilder, 14 ознак)
/// плюс 2 ознаки самої події (severity, critical). Ознаки стандартизуються за train.
/// Клас: 0 — штатна, 1 — аномальна, 2 — перехід до інциденту.
/// </summary>
public sealed class EventClassifier
{
    public const int ClassCount = 3;
    public const int ExtraFeatures = 2;
    public const int InputSize = FeatureBuilder.FeatureCount + ExtraFeatures;
    /// <summary>Обмеження довжини вікна (захист від дуже щільних сплесків).</summary>
    public const int MaxWindowRecords = 256;

    public int WindowMs { get; }
    private readonly Mlp model;
    private readonly double[] mean;
    private readonly double[] std;

    private EventClassifier(int windowMs, Mlp model, double[] mean, double[] std)
    {
        WindowMs = windowMs;
        this.model = model;
        this.mean = mean;
        this.std = std;
    }

    /// <summary>Навчання: стандартизація, балансування класів дублюванням, SGD.</summary>
    public static EventClassifier Train(IReadOnlyList<LogRecord> records, IReadOnlyList<int> labels,
        int windowMs = 60_000, int hidden = 16, int epochs = 40, double learningRate = 0.01, int seed = 1)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(labels);
        if (records.Count == 0 || records.Count != labels.Count)
            throw new ArgumentException("Потрібні непорожні records і labels однакової довжини.");

        double[][] x = BuildFeatures(records, windowMs);
        var (mean, std) = FitScaler(x);
        double[][] xs = x.Select(row => Scale(row, mean, std)).ToArray();

        // Балансування: кожен клас дублюємо до розміру найбільшого.
        var rng = new Random(seed);
        var byClass = Enumerable.Range(0, ClassCount)
            .Select(c => Enumerable.Range(0, labels.Count).Where(i => labels[i] == c).ToList())
            .ToList();
        int target = byClass.Max(l => l.Count);
        var idx = new List<int>();
        foreach (var list in byClass.Where(l => l.Count > 0))
            for (int k = 0; k < target; k++)
                idx.Add(list[k % list.Count]);
        // Перемішування (Фішер–Єйтс) — SGD чутливий до порядку прикладів.
        for (int i = idx.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (idx[i], idx[j]) = (idx[j], idx[i]);
        }

        var model = new Mlp(new[] { InputSize, hidden, ClassCount }, seed);
        model.Fit(idx.Select(i => xs[i]).ToArray(), idx.Select(i => labels[i]).ToArray(), epochs, learningRate);
        return new EventClassifier(windowMs, model, mean, std);
    }

    public int[] Predict(IReadOnlyList<LogRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        if (records.Count == 0) return Array.Empty<int>();
        double[][] x = BuildFeatures(records, WindowMs).Select(r => Scale(r, mean, std)).ToArray();
        return model.Predict(x);
    }

    /// <summary>Ознаки для кожної події. Вікно — записи з часом у [t_i − windowMs, t_i]
    /// у хронологічному порядку (вхід може бути не відсортованим).</summary>
    public static double[][] BuildFeatures(IReadOnlyList<LogRecord> records, int windowMs)
    {
        int n = records.Count;
        int[] order = Enumerable.Range(0, n)
            .OrderBy(i => records[i].TimestampMs).ThenBy(i => i).ToArray();

        var result = new double[n][];
        var window = new List<LogRecord>(MaxWindowRecords);
        int start = 0;
        for (int k = 0; k < n; k++)
        {
            var current = records[order[k]];
            while (start < k && (records[order[start]].TimestampMs < current.TimestampMs - windowMs
                                 || k - start >= MaxWindowRecords))
                start++;

            window.Clear();
            for (int j = start; j <= k; j++) window.Add(records[order[j]]);

            double[] f = new double[InputSize];
            FeatureBuilder.Build(window).CopyTo(f, 0);
            f[FeatureBuilder.FeatureCount] = current.Severity;
            f[FeatureBuilder.FeatureCount + 1] = current.Critical ? 1 : 0;
            result[order[k]] = f;
        }
        return result;
    }

    private static (double[] Mean, double[] Std) FitScaler(double[][] x)
    {
        int d = x[0].Length;
        var mean = new double[d];
        var std = new double[d];
        foreach (var row in x)
            for (int j = 0; j < d; j++) mean[j] += row[j];
        for (int j = 0; j < d; j++) mean[j] /= x.Length;
        foreach (var row in x)
            for (int j = 0; j < d; j++) std[j] += (row[j] - mean[j]) * (row[j] - mean[j]);
        for (int j = 0; j < d; j++)
        {
            std[j] = Math.Sqrt(std[j] / x.Length);
            if (std[j] < 1e-9) std[j] = 1; // стала ознака
        }
        return (mean, std);
    }

    private static double[] Scale(double[] row, double[] mean, double[] std)
    {
        var r = new double[row.Length];
        for (int j = 0; j < row.Length; j++) r[j] = (row[j] - mean[j]) / std[j];
        return r;
    }
}
