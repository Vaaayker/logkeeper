namespace LogKeeper.Integration.Replication;

/// <summary>
/// Сценарій відмов вузлів: які вузли втрачено повністю і які пакети пошкоджено
/// (вузол, номер пакета на вузлі). Вузли задаються за порядковим номером у списку вузлів.
/// </summary>
public sealed record FailureScenario(
    string Name,
    IReadOnlyList<string> LostNodes,
    IReadOnlyList<(string Node, int Packet)> CorruptedPackets)
{
    public static readonly string[] Names = { "none", "node-loss", "two-node-loss", "corrupt", "mixed" };

    /// <summary>
    /// none — без відмов; node-loss — втрачено перший вузол; two-node-loss — перші два;
    /// corrupt — пошкоджено перший пакет кожного вузла, крім першого (у ньому найцінніші події);
    /// mixed — втрачено останній вузол і пошкоджено перший пакет першого.
    /// </summary>
    public static FailureScenario Parse(string name, IReadOnlyList<string> nodes)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(nodes);
        if (nodes.Count == 0) throw new ArgumentException("Список вузлів порожній.", nameof(nodes));

        string key = name.Trim().ToLowerInvariant();
        var none = Array.Empty<(string, int)>();
        return key switch
        {
            "none" => new(key, Array.Empty<string>(), none),
            "node-loss" => new(key, new[] { nodes[0] }, none),
            "two-node-loss" => new(key, nodes.Take(Math.Min(2, nodes.Count)).ToArray(), none),
            "corrupt" => new(key, Array.Empty<string>(), nodes.Skip(1).Select(n => (n, 0)).ToArray()),
            "mixed" => new(key, nodes.Count > 1 ? new[] { nodes[^1] } : Array.Empty<string>(), new[] { (nodes[0], 0) }),
            _ => throw new ArgumentException(
                $"Невідомий сценарій '{name}'. Доступні: {string.Join(", ", Names)}.", nameof(name)),
        };
    }
}
