using LogKeeper.Core;

namespace LogKeeper.Integration;

public sealed record PipelineResult(double S, double R, double T, double Q);

/// <summary>Повний шлях даних: події -> клас -> стиснення -> копії -> порядок -> шифр -> вузли -> відновлення.</summary>
public static class Pipeline
{
    public static PipelineResult Run(IReadOnlyList<LogRecord> records, string scenario)
        => throw new NotImplementedException();
}
