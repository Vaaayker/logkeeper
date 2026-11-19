namespace LogKeeper.Integration.Replication;

/// <summary>
/// Вузли через replication.py (D-005). Обмін — файлами: пакети пишуться у
/// <c>{dir}/{node}/{NNNN}.log3</c>, потім викликається replication.py з аргументами за шаблоном,
/// і читаються ті файли, що залишилися (скрипт видаляє/псує їх відповідно до сценарію).
/// Шаблон аргументів задається ззовні (параметр <c>--replication-args</c> App або змінна
/// LOGKEEPER_REPLICATION_ARGS), бо точний CLI визначається REPLICATION_SPEC.md.
/// Плейсхолдери: {dir} — робоча папка, {scenario} — назва сценарію.
/// </summary>
public sealed class PythonReplicaStore : IReplicaStore, IDisposable
{
    private readonly string scriptPath;
    private readonly string argsTemplate;
    private readonly bool ownsDirectory;

    public string WorkDirectory { get; }
    /// <summary>Останній stdout replication.py (для журналу запуску).</summary>
    public string LastOutput { get; private set; } = "";

    public PythonReplicaStore(string scriptPath, string argsTemplate, string? workDirectory = null)
    {
        this.scriptPath = scriptPath;
        this.argsTemplate = argsTemplate;
        ownsDirectory = workDirectory is null;
        WorkDirectory = workDirectory ?? Path.Combine(Path.GetTempPath(), "logkeeper-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(WorkDirectory);
    }

    public void Write(string nodeId, IReadOnlyList<byte[]> packets)
    {
        string nodeDir = Path.Combine(WorkDirectory, nodeId);
        Directory.CreateDirectory(nodeDir);
        int start = Directory.GetFiles(nodeDir, "*.log3").Length;
        for (int i = 0; i < packets.Count; i++)
            File.WriteAllBytes(Path.Combine(nodeDir, $"{start + i:D4}.log3"), packets[i]);
    }

    public void Apply(FailureScenario scenario)
    {
        string[] args = argsTemplate
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Replace("{dir}", WorkDirectory).Replace("{scenario}", scenario.Name))
            .ToArray();
        LastOutput = ReplicationBridge.RunPython(scriptPath, args);
    }

    public IReadOnlyList<byte[]> ReadAll() =>
        Directory.GetFiles(WorkDirectory, "*.log3", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(File.ReadAllBytes)
            .ToList();

    public void Dispose()
    {
        if (ownsDirectory && Directory.Exists(WorkDirectory))
            Directory.Delete(WorkDirectory, recursive: true);
    }
}
