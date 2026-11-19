using LogKeeper.Classifier;
using LogKeeper.Integration.Config;
using LogKeeper.Integration.Data;
using LogKeeper.Integration.Replication;

namespace LogKeeper.Integration;

/// <summary>Усе, що налаштовує наскрізний запуск. За замовчуванням — з materials/ (або синтетики).</summary>
public sealed record PipelineOptions
{
    /// <summary>Пароль за замовчуванням — лише для демонстрації; справжній задається LOGKEEPER_PASSWORD.</summary>
    public const string DemoPassword = "logkeeper-demo-password";

    public TeamConfig Config { get; init; } = TeamConfig.Default;
    /// <summary>Навчений перцептрон; null — класи за правилом (абляція «без перцептрона»).</summary>
    public EventClassifier? Classifier { get; init; }
    public CopyStrategy Strategy { get; init; } = CopyStrategy.DynamicProgramming;
    /// <summary>null — значення за замовчуванням з BudgetFactor із team_config.</summary>
    public SchedulerParams? Params { get; init; }
    public string Password { get; init; } = Environment.GetEnvironmentVariable("LOGKEEPER_PASSWORD") ?? DemoPassword;
    /// <summary>Скільки записів в одному пакеті LOG3.</summary>
    public int PacketRecords { get; init; } = 128;
    public Func<IReplicaStore> StoreFactory { get; init; } = () => new InMemoryReplicaStore();
    public long DpCellLimit { get; init; } = CopyPlanning.DefaultDpCellLimit;

    public SchedulerParams EffectiveParams => Params ?? SchedulerParams.Default with { BudgetFactor = Config.BudgetFactor };

    /// <summary>Звідки взято дані навчання (для звіту).</summary>
    public string TrainingSource { get; init; } = "none";

    private static readonly Lazy<PipelineOptions> lazyDefault = new(() => FromMaterials(Materials.Locate()));
    public static PipelineOptions Default => lazyDefault.Value;

    /// <summary>team_config.json + перцептрон, навчений на train.csv (або синтетиці).
    /// Якщо задано LOGKEEPER_REPLICATION_ARGS і є replication.py — вузли через Python.</summary>
    public static PipelineOptions FromMaterials(Materials m, string? replicationArgs = null)
    {
        var config = TeamConfig.Load(m.PathOf("team_config.json"));
        var train = m.Train();
        var classifier = EventClassifier.Train(train.Data.Records, train.Data.Labels, config.WindowMs);

        replicationArgs ??= Environment.GetEnvironmentVariable("LOGKEEPER_REPLICATION_ARGS");
        Func<IReplicaStore> store = () => new InMemoryReplicaStore();
        if (!string.IsNullOrWhiteSpace(replicationArgs) && m.Has("replication.py"))
        {
            string script = m.PathOf("replication.py"), template = replicationArgs;
            store = () => new PythonReplicaStore(script, template);
        }

        return new PipelineOptions
        {
            Config = config,
            Classifier = classifier,
            StoreFactory = store,
            TrainingSource = train.Origin,
        };
    }
}
