using LogKeeper.Core;

namespace LogKeeper.Classifier;

/// <summary>Ознаки з вікном історії (частота подій, зміна metric_value, нестабільність category тощо).</summary>
public static class FeatureBuilder
{
    public static double[] Build(IReadOnlyList<LogRecord> window) => throw new NotImplementedException();
}
