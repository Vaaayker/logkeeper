using LogKeeper.Core;
using LogKeeper.Integration;
using LogKeeper.Integration.Config;
using LogKeeper.Integration.Data;
using LogKeeper.Integration.Replication;

namespace LogKeeper.Tests.Integration;

public class PipelineTests
{
    private static readonly List<LogRecord> Session = SyntheticLog.Generate(400, 5).Records;

    private static PipelineOptions Options(double budgetFactor, Func<IReplicaStore>? store = null) => new()
    {
        Config = TeamConfig.Default,
        Classifier = null, // правило замість перцептрона — швидше для тесту
        Params = SchedulerParams.Default with { BudgetFactor = budgetFactor },
        PacketRecords = 64,
        StoreFactory = store ?? (() => new InMemoryReplicaStore()),
    };

    [Fact]
    public void NoFailures_FullBudget_RecoversWholeLog()
    {
        var result = Pipeline.Run(Session, "none", Options(3.0));

        Assert.Equal(1.0, result.R);
        Assert.Equal(1.0, result.S);
        Assert.Equal(0, result.PacketsRejected);
        Assert.Equal(0, result.SpuriousRecords);
        Assert.Equal(Session.Distinct().Count(), result.RecoveredEvents);
        Assert.All(result.Copies, c => Assert.Equal(3, c));
        Assert.Equal(0.0, result.T, 10);
    }

    [Fact]
    public void NoFailures_RecoversExactlyEventsWithCopies()
    {
        var result = Pipeline.Run(Session, "none", Options(1.0));

        var withCopies = Session.Where((r, i) => result.Copies[i] > 0).Distinct().Count();
        Assert.Equal(withCopies, result.RecoveredEvents);
        Assert.InRange(result.Q, 0, 1);
    }

    [Fact]
    public void CorruptedReplica_IsRejectedByHmac_AndDataComesFromOtherCopies()
    {
        var result = Pipeline.Run(Session, "corrupt", Options(3.0));

        Assert.True(result.PacketsRejected >= 1);
        Assert.Equal(0, result.SpuriousRecords);
        Assert.Equal(1.0, result.R); // кожна подія має 3 копії на різних вузлах
    }

    [Fact]
    public void TwoNodesLost_CriticalEventsSurviveBetterThanAverage()
    {
        var result = Pipeline.Run(Session, "two-node-loss", Options(1.5));

        Assert.Equal(0, result.SpuriousRecords);
        Assert.True(result.S >= result.R, $"S={result.S}, R={result.R}");
    }

    [Fact]
    public void DpPlan_HasAtLeastGreedyUtility()
    {
        var dp = Pipeline.Run(Session, "none", Options(1.2));
        var greedy = Pipeline.Run(Session, "none", Options(1.2) with { Strategy = CopyStrategy.Greedy });

        Assert.True(dp.PlanUtility >= greedy.PlanUtility);
    }

    [Fact]
    public void UnknownScenario_Throws()
    {
        Assert.Throws<ArgumentException>(() => Pipeline.Run(Session, "meteor", Options(1.5)));
    }

    [Fact]
    public void FailureScenario_ParsesAllNames()
    {
        var nodes = new[] { "A", "B", "C" };
        foreach (var name in FailureScenario.Names)
            Assert.Equal(name, FailureScenario.Parse(name, nodes).Name);
        Assert.Equal(new[] { "A", "B" }, FailureScenario.Parse("two-node-loss", nodes).LostNodes);
    }

    [Fact]
    public void TeamConfig_ReadsNestedKeys_AndFallsBackToDefaults()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cfg-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """{ "metric": { "Alpha": 0.6, "beta": 0.25 }, "nodes": [{"id":"X"},{"id":"Y"}] }""");
        try
        {
            var c = TeamConfig.Load(path);
            Assert.Equal(0.6, c.Alpha);
            Assert.Equal(0.25, c.Beta);
            Assert.Equal(TeamConfig.Default.Gamma, c.Gamma);
            Assert.Equal(new[] { "X", "Y" }, c.Nodes);
        }
        finally { File.Delete(path); }

        Assert.Same(TeamConfig.Default, TeamConfig.Load("no-such-file.json"));
    }

    [Fact]
    public void RecordCsv_SaveLoad_RoundTrip()
    {
        var data = SyntheticLog.Generate(50, 3);
        string path = Path.Combine(Path.GetTempPath(), $"rec-{Guid.NewGuid():N}.csv");
        try
        {
            RecordCsv.Save(path, data.Records, data.Labels);
            var loaded = RecordCsv.Load(path);
            Assert.Equal(data.Records, loaded.Records);
            Assert.Equal(data.Labels, loaded.Labels);
        }
        finally { File.Delete(path); }
    }
}
