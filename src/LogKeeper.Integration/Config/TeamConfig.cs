using System.Globalization;
using System.Text.Json;

namespace LogKeeper.Integration.Config;

/// <summary>
/// Параметри команди з materials/team_config.json. Оригінальна схема файла ще невідома (D-007),
/// тож ключі шукаються за назвою на будь-якій глибині JSON (без урахування регістру).
/// Якщо файла немає або ключа немає — використовується значення за замовчуванням.
/// </summary>
public sealed record TeamConfig
{
    public double Alpha { get; init; } = 0.5;
    public double Beta { get; init; } = 0.3;
    public double Gamma { get; init; } = 0.2;
    /// <summary>Бюджет = BudgetFactor × (сумарна вага однієї копії всіх подій).</summary>
    public double BudgetFactor { get; init; } = 1.5;
    /// <summary>Ймовірність відмови одного вузла — для корисності u_i(r) = v_i · (1 − p^r).</summary>
    public double NodeFailureProbability { get; init; } = 0.2;
    public IReadOnlyList<string> Nodes { get; init; } = new[] { "A", "B", "C" };
    /// <summary>Тривалість вікна історії для ознак класифікатора, мс.</summary>
    public int WindowMs { get; init; } = 60_000;
    /// <summary>Звідки взято значення (шлях до файла або "defaults").</summary>
    public string Source { get; init; } = "defaults";

    public static TeamConfig Default { get; } = new();

    public static TeamConfig Load(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return Default;

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var d = Default;
        return new TeamConfig
        {
            Alpha = FindNumber(root, "alpha") ?? d.Alpha,
            Beta = FindNumber(root, "beta") ?? d.Beta,
            Gamma = FindNumber(root, "gamma") ?? d.Gamma,
            BudgetFactor = FindNumber(root, "budget_factor", "budgetFactor", "budget") ?? d.BudgetFactor,
            NodeFailureProbability = FindNumber(root, "node_failure_prob", "failure_prob", "p_fail", "p_failure")
                                     ?? d.NodeFailureProbability,
            WindowMs = (int)(FindNumber(root, "window_ms", "windowMs") ?? d.WindowMs),
            Nodes = FindNodes(root) ?? d.Nodes,
            Source = path,
        };
    }

    private static double? FindNumber(JsonElement e, params string[] names)
    {
        var found = Find(e, names);
        if (found is not { } v) return null;
        return v.ValueKind switch
        {
            JsonValueKind.Number => v.GetDouble(),
            JsonValueKind.String when double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) => x,
            _ => null,
        };
    }

    private static IReadOnlyList<string>? FindNodes(JsonElement root)
    {
        if (Find(root, "nodes") is not { ValueKind: JsonValueKind.Array } arr) return null;
        var ids = new List<string>();
        foreach (var item in arr.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String) ids.Add(item.GetString()!);
            else if (item.ValueKind == JsonValueKind.Object && Find(item, "id", "name") is { ValueKind: JsonValueKind.String } id)
                ids.Add(id.GetString()!);
        }
        return ids.Count > 0 ? ids : null;
    }

    private static JsonElement? Find(JsonElement e, params string[] names)
    {
        if (e.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in e.EnumerateObject())
                if (names.Any(n => string.Equals(n, p.Name, StringComparison.OrdinalIgnoreCase)))
                    return p.Value;
            foreach (var p in e.EnumerateObject())
                if (Find(p.Value, names) is { } nested) return nested;
        }
        else if (e.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in e.EnumerateArray())
                if (Find(item, names) is { } nested) return nested;
        }
        return null;
    }
}
