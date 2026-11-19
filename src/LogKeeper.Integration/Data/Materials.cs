using LogKeeper.Core;

namespace LogKeeper.Integration.Data;

/// <summary>
/// Доступ до materials/ (файли викладача). Кожен метод повідомляє, звідки взято дані:
/// якщо потрібного файла немає, повертається синтетика з <see cref="SyntheticLog"/> (D-008).
/// </summary>
public sealed class Materials
{
    public const int SyntheticTrainSeed = 11;
    public const int SyntheticValidSeed = 22;
    public const int SyntheticSessionSeed = 33;

    public string Directory { get; }

    public Materials(string directory) => Directory = directory;

    public string PathOf(string file) => Path.Combine(Directory, file);
    public bool Has(string file) => File.Exists(PathOf(file));

    /// <summary>Шукає materials/ поруч із LogKeeper.sln, піднімаючись від поточної папки та папки збірки.</summary>
    public static Materials Locate()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "LogKeeper.sln")))
                    return new Materials(Path.Combine(dir.FullName, "materials"));
        }
        return new Materials(Path.Combine(Environment.CurrentDirectory, "materials"));
    }

    public sealed record Source<T>(T Data, string Origin);

    public Source<SyntheticLog.Labeled> Train() => Labeled("train.csv", 3000, SyntheticTrainSeed);
    public Source<SyntheticLog.Labeled> Valid() => Labeled("valid.csv", 1500, SyntheticValidSeed);

    /// <summary>Сесія для наскрізного запуску: session.bin, далі session.csv, далі синтетика.</summary>
    public Source<List<LogRecord>> Session(int syntheticCount = 2000)
    {
        if (Has("session.bin"))
            return new(RecordCodec.ParseMany(File.ReadAllBytes(PathOf("session.bin"))), PathOf("session.bin"));
        if (Has("session.csv"))
            return new(RecordCsv.Load(PathOf("session.csv")).Records, PathOf("session.csv"));
        return new(SyntheticLog.Generate(syntheticCount, SyntheticSessionSeed).Records,
            $"synthetic(n={syntheticCount}, seed={SyntheticSessionSeed})");
    }

    /// <summary>Довільний вхідний файл (.bin — 16-байтові записи підряд, .csv — таблиця).</summary>
    public static List<LogRecord> LoadRecords(string path) =>
        Path.GetExtension(path).Equals(".bin", StringComparison.OrdinalIgnoreCase)
            ? RecordCodec.ParseMany(File.ReadAllBytes(path))
            : RecordCsv.Load(path).Records;

    private Source<SyntheticLog.Labeled> Labeled(string file, int n, int seed)
    {
        if (Has(file))
        {
            var t = RecordCsv.Load(PathOf(file));
            if (t.Labels is null)
                throw new FormatException($"{file}: немає колонки class.");
            return new(new SyntheticLog.Labeled(t.Records, t.Labels), PathOf(file));
        }
        return new(SyntheticLog.Generate(n, seed), $"synthetic(n={n}, seed={seed})");
    }
}
