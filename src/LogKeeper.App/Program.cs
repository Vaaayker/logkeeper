using System.Globalization;
using LogKeeper.Core;
using LogKeeper.Integration;
using LogKeeper.Integration.Data;
using LogKeeper.Integration.Replication;

// LogKeeper — наскрізний запуск: події -> класифікація -> стиснення -> копії -> порядок ->
// шифрування -> вузли -> відновлення -> метрики S, R, T, Q.
//
//   dotnet run --project src/LogKeeper.App -- [--scenario <name>|all] [--input <file.bin|file.csv>]
//       [--materials <dir>] [--strategy dp|greedy] [--replication-args "<template>"]

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string scenario = "mixed";
string? input = null, materialsDir = null, replicationArgs = null;
var strategy = CopyStrategy.DynamicProgramming;

for (int i = 0; i < args.Length; i++)
{
    string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"Після {args[i]} потрібне значення.");
    switch (args[i])
    {
        case "--scenario": scenario = Next(); break;
        case "--input": input = Next(); break;
        case "--materials": materialsDir = Next(); break;
        case "--replication-args": replicationArgs = Next(); break;
        case "--strategy":
            strategy = Next().ToLowerInvariant() switch
            {
                "dp" => CopyStrategy.DynamicProgramming,
                "greedy" => CopyStrategy.Greedy,
                var s => throw new ArgumentException($"Невідома стратегія '{s}' (dp | greedy)."),
            };
            break;
        case "-h" or "--help":
            Console.WriteLine("Використання: LogKeeper.App [--scenario <" + string.Join("|", FailureScenario.Names) +
                              "|all>] [--input file] [--materials dir] [--strategy dp|greedy] [--replication-args \"{dir} {scenario}\"]");
            return 0;
        default:
            Console.Error.WriteLine($"Невідомий аргумент: {args[i]} (див. --help)");
            return 2;
    }
}

try
{
    var materials = materialsDir is null ? Materials.Locate() : new Materials(materialsDir);
    replicationArgs ??= Environment.GetEnvironmentVariable("LOGKEEPER_REPLICATION_ARGS");
    var options = PipelineOptions.FromMaterials(materials, replicationArgs) with { Strategy = strategy };

    var session = input is null
        ? materials.Session()
        : new Materials.Source<List<LogRecord>>(Materials.LoadRecords(input), input);

    bool python = !string.IsNullOrWhiteSpace(replicationArgs) && materials.Has("replication.py");
    Console.WriteLine("LogKeeper — наскрізний запуск");
    Console.WriteLine($"  сесія:          {session.Origin} ({session.Data.Count} подій)");
    Console.WriteLine($"  team_config:    {options.Config.Source}");
    Console.WriteLine($"  навчання MLP:   {options.TrainingSource}");
    Console.WriteLine($"  вузли:          {string.Join(", ", options.Config.Nodes)} ({(python ? "replication.py" : "у пам'яті")})");
    Console.WriteLine($"  стратегія:      {strategy}; Q = {options.Config.Alpha}·S + {options.Config.Beta}·R + {options.Config.Gamma}·T");
    Console.WriteLine();

    var scenarios = scenario == "all" ? FailureScenario.Names : new[] { scenario };
    Console.WriteLine($"{"сценарій",-14} {"S",7} {"R",7} {"T",7} {"Q",7} {"відкинуто",10} {"час, мс",9}");
    foreach (var sc in scenarios)
    {
        var r = Pipeline.Run(session.Data, sc, options);
        Console.WriteLine($"{r.Scenario,-14} {r.S,7:F4} {r.R,7:F4} {r.T,7:F4} {r.Q,7:F4} {r.PacketsRejected,10} {r.ElapsedMs,9:F0}");
    }
    return 0;
}
catch (Exception ex) when (ex is ArgumentException or FormatException or IOException or ReplicationException)
{
    Console.Error.WriteLine($"Помилка: {ex.Message}");
    return 1;
}
