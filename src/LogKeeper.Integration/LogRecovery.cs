using LogKeeper.Core;

namespace LogKeeper.Integration;

/// <summary>Відновлення журналу з наявних реплік і усунення дублікатів.</summary>
public static class LogRecovery
{
    public static List<LogRecord> Recover(IReadOnlyList<byte[]> packets, string password)
        => throw new NotImplementedException();
}
