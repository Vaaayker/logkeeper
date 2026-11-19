namespace LogKeeper.Integration.Replication;

/// <summary>Сховище реплік на вузлах: запис пакетів, застосування відмов, читання того, що вціліло.</summary>
public interface IReplicaStore
{
    void Write(string nodeId, IReadOnlyList<byte[]> packets);
    void Apply(FailureScenario scenario);
    IReadOnlyList<byte[]> ReadAll();
}

/// <summary>Вузли в пам'яті: втрачений вузол зникає повністю, пошкоджений пакет отримує
/// інвертований байт посередині шифротексту (HMAC має його відкинути).</summary>
public sealed class InMemoryReplicaStore : IReplicaStore
{
    private readonly Dictionary<string, List<byte[]>> nodes = new();

    public void Write(string nodeId, IReadOnlyList<byte[]> packets)
    {
        if (!nodes.TryGetValue(nodeId, out var list))
            nodes[nodeId] = list = new List<byte[]>();
        list.AddRange(packets.Select(p => (byte[])p.Clone()));
    }

    public void Apply(FailureScenario scenario)
    {
        foreach (var node in scenario.LostNodes)
            nodes.Remove(node);
        foreach (var (node, index) in scenario.CorruptedPackets)
            if (nodes.TryGetValue(node, out var list) && index < list.Count)
                Corrupt(list[index]);
    }

    public IReadOnlyList<byte[]> ReadAll() => nodes.Values.SelectMany(l => l).ToList();

    public IReadOnlyDictionary<string, int> PacketCounts() => nodes.ToDictionary(kv => kv.Key, kv => kv.Value.Count);

    public static void Corrupt(byte[] packet)
    {
        if (packet.Length == 0) return;
        packet[packet.Length / 2] ^= 0xFF;
    }
}
