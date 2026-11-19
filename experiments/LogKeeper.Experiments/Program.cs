using System.Diagnostics;
using System.Globalization;
using LogKeeper.Classifier;
using LogKeeper.Core;
using LogKeeper.Experiments;
using LogKeeper.Integration;
using LogKeeper.Integration.Data;
using LogKeeper.Integration.Replication;
using LogKeeper.Scheduler;
using LogKeeper.Security;

// dotnet run --project experiments/LogKeeper.Experiments -c Release -- <dp-vs-greedy|classifier|ga|ablation|final|all>

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var materials = Materials.Locate();
string resultsDir = Path.Combine(Path.GetDirectoryName(materials.Directory)!, "results");
string gaParamsPath = Path.Combine(resultsDir, "ga_best_params.csv");

var experiments = new Dictionary<string, Action>
{
    ["dp-vs-greedy"] = DpVsGreedy,
    ["classifier"] = ClassifierExperiment,
    ["ga"] = GaExperiment,
    ["ablation"] = Ablation,
    ["final"] = Final,
};

string which = args.Length > 0 ? args[0] : "all";
if (which == "all")
{
    foreach (var (name, run) in experiments) Timed(name, run);
    return 0;
}
if (!experiments.TryGetValue(which, out var single))
{
    Console.Error.WriteLine($"Невідомий експеримент '{which}'. Доступні: {string.Join(", ", experiments.Keys)}, all.");
    return 2;
}
Timed(which, single);
return 0;

void Timed(string name, Action run)
{
    Console.WriteLine($"=== {name} ===");
    var sw = Stopwatch.StartNew();
    run();
    Console.WriteLine($"=== {name}: {sw.Elapsed.TotalSeconds:F1} с ===\n");
}

// ---------------------------------------------------------------- dp-vs-greedy
void DpVsGreedy()
{
    var t = new ResultTable("dp_vs_greedy", "ДП проти жадібного: сумарна корисність і час",
        "n", "budget_factor", "budget", "utility_dp", "utility_greedy", "greedy/dp", "time_dp_ms", "time_greedy_ms");
    var session = SyntheticLog.Generate(2000, Materials.SyntheticSessionSeed);

    foreach (int n in new[] { 100, 500, 1000, 2000 })
    {
        var records = session.Records.Take(n).ToList();
        int[] classes = session.Labels.Take(n).ToArray();
        foreach (double factor in new[] { 0.5, 1.0, 1.5, 2.0 })
        {
            var p = CopyPlanning.BuildProblem(records, classes, SchedulerParams.Default with { BudgetFactor = factor }, 0.2);
            var (dp, tDp) = Measure(() => CopyPlanning.Choose(p, CopyStrategy.DynamicProgramming));
            var (gr, tGr) = Measure(() => CopyPlanning.Choose(p, CopyStrategy.Greedy));
            long uDp = p.Utility(dp), uGr = p.Utility(gr);
            t.Add(n, factor, p.Budget, uDp, uGr, (double)uGr / uDp, tDp, tGr);
        }
    }

    // Випадкові дрібні задачі з «незручними» вагами — тут жадібний помиляється частіше.
    var rng = new Random(2024);
    int trials = 2000, worse = 0;
    double gapSum = 0, worstRatio = 1;
    for (int k = 0; k < trials; k++)
    {
        int n = rng.Next(3, 15);
        int[] w = Enumerable.Range(0, n).Select(_ => rng.Next(1, 20)).ToArray();
        int[][] u = Enumerable.Range(0, n).Select(_ =>
        {
            int a = rng.Next(1, 50), b = rng.Next(0, 30), c = rng.Next(0, 15);
            return new[] { 0, a, a + b, a + b + c };
        }).ToArray();
        int budget = rng.Next(5, w.Sum() * 2);
        long dpU = Knapsack.ChooseCopies(w, u, budget).Select((r, i) => (long)u[i][r]).Sum();
        long grU = GreedyScheduler.ChooseCopies(w, u, budget).Select((r, i) => (long)u[i][r]).Sum();
        if (grU < dpU)
        {
            worse++;
            gapSum += 1 - (double)grU / dpU;
            worstRatio = Math.Min(worstRatio, (double)grU / dpU);
        }
    }
    t.Note($"Корисність: u_i(r) = 100 · v_i · (1 − 0.2^r); ваги — байти після Гаффмана; класи — мітки синтетики.");
    t.Note($"budget — у одиницях задачі після масштабування: якщо n · budget > {CopyPlanning.DefaultDpCellLimit:N0} клітинок, " +
           "ваги й бюджет діляться на спільний дільник (тому для n = 2000 значення не ростуть пропорційно budget_factor).");
    t.Note($"Випадкові дрібні задачі ({trials} шт., n = 3..14): жадібний гірший за ДП у {worse} ({100.0 * worse / trials:F1}%), " +
           $"середній програш у цих випадках {100 * (worse == 0 ? 0 : gapSum / worse):F2}%, найгірше відношення {worstRatio:F3}. ДП ніколи не гірший.");
    t.Save(resultsDir);
}

// ---------------------------------------------------------------- classifier
void ClassifierExperiment()
{
    var config = PipelineOptions.FromMaterials(materials).Config;
    var train = materials.Train();
    var valid = materials.Valid();

    var sw = Stopwatch.StartNew();
    var clf = EventClassifier.Train(train.Data.Records, train.Data.Labels, config.WindowMs);
    double trainMs = sw.Elapsed.TotalMilliseconds;
    int[] pred = clf.Predict(valid.Data.Records);
    int[] rule = CopyPlanning.RuleBasedClasses(valid.Data.Records);

    var m = ClassificationMetrics.ConfusionMatrix(valid.Data.Labels, pred, 3);
    var mRule = ClassificationMetrics.ConfusionMatrix(valid.Data.Labels, rule, 3);

    var cm = new ResultTable("classifier_confusion", "Матриця помилок перцептрона на valid (рядок — істинний клас)",
        "true \\ pred", "0", "1", "2", "F1");
    double[] f1 = ClassificationMetrics.PerClassF1(m);
    for (int c = 0; c < 3; c++) cm.Add(c, m[c, 0], m[c, 1], m[c, 2], f1[c]);
    cm.Note($"train: {train.Origin}; valid: {valid.Origin}; вікно {config.WindowMs} мс; MLP 16→16→3, 40 епох, lr = 0.01.");
    cm.Save(resultsDir);

    var s = new ResultTable("classifier", "Класифікатор подій: якість на valid",
        "model", "accuracy", "macro_F1", "F1_class0", "F1_class1", "F1_class2", "train_ms");
    s.Add("MLP (перцептрон)", ClassificationMetrics.Accuracy(m), ClassificationMetrics.MacroF1(m), f1[0], f1[1], f1[2], trainMs);
    double[] f1Rule = ClassificationMetrics.PerClassF1(mRule);
    s.Add("правило severity/critical", ClassificationMetrics.Accuracy(mRule), ClassificationMetrics.MacroF1(mRule),
        f1Rule[0], f1Rule[1], f1Rule[2], 0.0);
    s.Note($"train: {train.Origin} ({train.Data.Records.Count}); valid: {valid.Origin} ({valid.Data.Records.Count}).");
    s.Save(resultsDir);
}

// ---------------------------------------------------------------- ga
void GaExperiment()
{
    int[] seeds = { 1, 2, 3, 4, 5 };

    // 1) Тестова функція Растригіна (10 генів): збіжність ГА проти випадкового пошуку.
    static double Rastrigin(double[] g) =>
        -g.Sum(v => { double x = (v - 0.5) * 10.24; return x * x - 10 * Math.Cos(2 * Math.PI * x) + 10; });
    int pop = 40, gens = 60;
    int evals = GeneticAlgorithm.EvaluationsFor(pop, gens);
    int[] checkpoints = { 40, 250, 500, 1000, 1500, evals };
    var conv = new ResultTable("ga_convergence", "Збіжність: ГА проти випадкового пошуку (Растригін, 10 генів, середнє за 5 seed)",
        new[] { "evaluations", "GA_best", "random_best" });
    var gaRuns = seeds.Select(s => GeneticAlgorithm.Run(Rastrigin, 10, pop, gens, s)).ToList();
    var rsRuns = seeds.Select(s => GeneticAlgorithm.RandomSearch(Rastrigin, 10, evals, s)).ToList();
    foreach (int c in checkpoints)
        conv.Add(c, gaRuns.Average(r => r.History[c - 1]), rsRuns.Average(r => r.History[c - 1]));
    conv.Note("Fitness = −Rastrigin (максимум 0). Однакова кількість обчислень fitness для обох методів.");
    conv.Note($"ГА: популяція {pop}, {gens} поколінь, турнір 3, кросовер 0.9 (рівномірний), мутація 0.1 (σ = 0.1), еліта 2.");
    conv.Save(resultsDir);

    // 2) Налаштування параметрів системи: fitness = середній Q за сценаріями відмов.
    var session = materials.Session(syntheticCount: 300).Data.Take(300).ToList();
    var baseOptions = PipelineOptions.FromMaterials(materials) with { PacketRecords = 1024 };
    var tuneScenarios = new[] { "node-loss", "two-node-loss", "mixed" };
    double Fitness(double[] g) => tuneScenarios.Average(sc =>
        Pipeline.Run(session, sc, baseOptions with { Params = ParamsFromGenes(g) }).Q);

    int tPop = 12, tGens = 8;
    int tEvals = GeneticAlgorithm.EvaluationsFor(tPop, tGens);
    var ga = GeneticAlgorithm.Run(Fitness, 5, tPop, tGens, seed: 1);
    var rs = GeneticAlgorithm.RandomSearch(Fitness, 5, tEvals, seed: 1);
    double baseline = tuneScenarios.Average(sc => Pipeline.Run(session, sc, baseOptions).Q);

    var tune = new ResultTable("ga_tuning", "Підбір параметрів системи ГА (fitness = середній Q за node-loss, two-node-loss, mixed)",
        "method", "evaluations", "best_Q", "ValueNormal", "ValueAnomaly", "ValuePreIncident", "CriticalBonus", "BudgetFactor");
    var def = baseOptions.EffectiveParams;
    tune.Add("параметри за замовчуванням", 1, baseline, def.ValueNormal, def.ValueAnomaly, def.ValuePreIncident, def.CriticalBonus, def.BudgetFactor);
    foreach (var (name, r) in new[] { ("ГА", ga), ("випадковий пошук", rs) })
    {
        var p = ParamsFromGenes(r.BestGenes);
        tune.Add(name, r.Evaluations, r.BestFitness, p.ValueNormal, p.ValueAnomaly, p.ValuePreIncident, p.CriticalBonus, p.BudgetFactor);
    }
    tune.Note($"Сесія: перші 300 подій ({materials.Session(300).Origin}); ГА: популяція {tPop}, {tGens} поколінь, seed 1.");
    tune.Note("Найкращі параметри ГА збережено в results/ga_best_params.csv — їх використовують ablation і final.");
    tune.Save(resultsDir);

    var best = ParamsFromGenes(ga.BestGenes);
    File.WriteAllText(gaParamsPath,
        "ValueNormal,ValueAnomaly,ValuePreIncident,CriticalBonus,BudgetFactor\n" +
        string.Join(",", new[] { best.ValueNormal, best.ValueAnomaly, best.ValuePreIncident, best.CriticalBonus, best.BudgetFactor }
            .Select(v => v.ToString("R", CultureInfo.InvariantCulture))) + "\n");

    var curve = new ResultTable("ga_tuning_convergence", "Збіжність підбору параметрів: найкращий Q після k обчислень",
        "evaluations", "GA_best_Q", "random_best_Q");
    foreach (int c in new[] { 1, 6, 12, 24, 48, 72, tEvals }.Where(c => c <= tEvals).Distinct())
        curve.Add(c, ga.History[c - 1], rs.History[c - 1]);
    curve.Save(resultsDir);
}

// Гени [0,1) -> параметри в розумних межах.
static SchedulerParams ParamsFromGenes(double[] g) => new(
    ValueNormal: 0.5 + g[0] * 2.5,
    ValueAnomaly: 1 + g[1] * 9,
    ValuePreIncident: 2 + g[2] * 28,
    CriticalBonus: g[3] * 30,
    BudgetFactor: 0.5 + g[4] * 2.5);

SchedulerParams? LoadGaParams()
{
    if (!File.Exists(gaParamsPath)) return null;
    var v = File.ReadAllLines(gaParamsPath)[1].Split(',').Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
    return new SchedulerParams(v[0], v[1], v[2], v[3], v[4]);
}

// ---------------------------------------------------------------- ablation
void Ablation()
{
    var full = PipelineOptions.FromMaterials(materials);
    var gaParams = LoadGaParams();
    if (gaParams is null) Console.WriteLine("results/ga_best_params.csv немає — спершу запустіть `ga`; поки що беруться параметри за замовчуванням.");
    var withGa = full with { Params = gaParams };
    var session = materials.Session();

    var configs = new (string Name, PipelineOptions Options)[]
    {
        ("повна система (MLP + ДП + ГА)", withGa),
        ("без перцептрона (правило)", withGa with { Classifier = null }),
        ("без ДП (жадібний)", withGa with { Strategy = CopyStrategy.Greedy }),
        ("без ГА (параметри за замовч.)", full),
    };

    var t = new ResultTable("ablation", "Абляція: середні метрики за 5 сценаріями відмов",
        "configuration", "S", "R", "T", "Q", "Q_two_node_loss", "plan_utility");
    foreach (var (name, o) in configs)
    {
        var rs = FailureScenario.Names.Select(sc => Pipeline.Run(session.Data, sc, o)).ToList();
        t.Add(name, rs.Average(r => r.S), rs.Average(r => r.R), rs.Average(r => r.T), rs.Average(r => r.Q),
            rs.Single(r => r.Scenario == "two-node-loss").Q, rs[0].PlanUtility);
    }
    t.Note($"Сесія: {session.Origin} ({session.Data.Count} подій); сценарії: {string.Join(", ", FailureScenario.Names)}.");
    t.Note("plan_utility рахується за класами конкретної конфігурації, тож між рядками з різними класами/параметрами напряму не порівнюється.");
    t.Save(resultsDir);
}

// ---------------------------------------------------------------- final
void Final()
{
    var options = PipelineOptions.FromMaterials(materials) with { Params = LoadGaParams() };
    var session = materials.Session();

    var t = new ResultTable("final", "Фінальні метрики S, R, T, Q",
        "scenario", "S", "R", "T", "Q", "events", "recovered", "critical", "critical_recovered",
        "packets_written", "packets_rejected", "bytes_raw", "bytes_huffman", "bytes_written", "time_ms");
    var all = new List<PipelineResult>();
    foreach (var sc in FailureScenario.Names)
    {
        var r = Pipeline.Run(session.Data, sc, options);
        all.Add(r);
        t.Add(sc, r.S, r.R, r.T, r.Q, r.TotalEvents, r.RecoveredEvents, r.CriticalEvents, r.CriticalRecovered,
            r.PacketsWritten, r.PacketsRejected, r.BytesRaw, r.BytesCompressed, r.BytesWritten, r.ElapsedMs);
    }
    t.Add("середнє", all.Average(r => r.S), all.Average(r => r.R), all.Average(r => r.T), all.Average(r => r.Q),
        "", "", "", "", "", "", "", "", "", all.Average(r => r.ElapsedMs));

    var p = options.EffectiveParams;
    int[] dist = Enumerable.Range(0, 4).Select(c => all[0].Copies.Count(x => x == c)).ToArray();
    t.Note($"Сесія: {session.Origin} ({session.Data.Count} подій); team_config: {options.Config.Source}; навчання: {options.TrainingSource}.");
    t.Note($"Q = {options.Config.Alpha}·S + {options.Config.Beta}·R + {options.Config.Gamma}·T; параметри: {p}.");
    t.Note($"Розподіл копій r = 0/1/2/3: {string.Join(" / ", dist)}. Вузли: у пам'яті (replication.py ще не отримано).");
    t.Save(resultsDir);
}

static (T Result, double Ms) Measure<T>(Func<T> f)
{
    var sw = Stopwatch.StartNew();
    var r = f();
    return (r, sw.Elapsed.TotalMilliseconds);
}
