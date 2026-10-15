namespace LogKeeper.Integration.Replication;

/// <summary>
/// Міст до replication.py (готовий файл на Python, змінювати його заборонено).
/// Ідея: запускати `python replication.py ...` як окремий процес (System.Diagnostics.Process)
/// й обмінюватися даними через файли або stdin/stdout.
/// Деталі — після прочитання REPLICATION_SPEC.md і самого replication.py.
/// </summary>
public static class ReplicationBridge
{
    public static string RunPython(string scriptPath, params string[] args)
        => throw new NotImplementedException();
}
