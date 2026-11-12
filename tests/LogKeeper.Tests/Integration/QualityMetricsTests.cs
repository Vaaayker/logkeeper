using LogKeeper.Integration;

namespace LogKeeper.Tests.Integration;

public class QualityMetricsTests
{
    [Fact]
    public void ComputeQ_WeightedSum()
    {
        // 0.5*0.8 + 0.3*0.6 + 0.2*0.9 = 0.4 + 0.18 + 0.18
        Assert.Equal(0.76, QualityMetrics.ComputeQ(0.8, 0.6, 0.9, 0.5, 0.3, 0.2), 10);
    }

    [Theory]
    [InlineData(1, 0, 0, 0.7)]
    [InlineData(0, 1, 0, 0.2)]
    [InlineData(0, 0, 1, 0.1)]
    public void ComputeQ_EachWeightAppliesToItsMetric(double alpha, double beta, double gamma, double expected)
    {
        Assert.Equal(expected, QualityMetrics.ComputeQ(0.7, 0.2, 0.1, alpha, beta, gamma), 10);
    }

    [Fact]
    public void ComputeQ_ZeroWeights_ReturnsZero()
    {
        Assert.Equal(0, QualityMetrics.ComputeQ(0.9, 0.9, 0.9, 0, 0, 0));
    }
}
