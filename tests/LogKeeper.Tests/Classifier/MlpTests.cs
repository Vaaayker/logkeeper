using LogKeeper.Classifier;
using Xunit;

namespace LogKeeper.Tests.Classifier;

public class MlpTests
{
    [Fact]
    public void Relu_ShouldReplaceNegativeValuesWithZero()
    {
        double[] input = { -1.0, 0.0, 2.0 };

        double[] result = Mlp.Relu(input);

        Assert.Equal(new[] { 0.0, 0.0, 2.0 }, result);
    }

    [Fact]
    public void Softmax_ShouldSumToOne()
    {
        double[] input = { 1.0, 2.0, 3.0 };

        double[] result = Mlp.Softmax(input);

        double sum = 0.0;

        for (int i = 0; i < result.Length; i++)
            sum += result[i];

        Assert.InRange(sum, 1.0 - 1e-9, 1.0 + 1e-9);
    }

    [Fact]
    public void Softmax_WithLargeValues_ShouldBeStable()
    {
        double[] input = { 1000.0, 1000.0, 1000.0 };

        double[] result = Mlp.Softmax(input);

        for (int i = 0; i < result.Length; i++)
        {
            Assert.False(double.IsNaN(result[i]));

            Assert.InRange(
                result[i],
                (1.0 / 3.0) - 1e-9,
                (1.0 / 3.0) + 1e-9);
        }
    }

    [Fact]
    public void Forward_WithSameSeed_ShouldReturnSameResult()
    {
        var first = new Mlp(new[] { 2, 4, 3 }, 42);
        var second = new Mlp(new[] { 2, 4, 3 }, 42);

        double[] input = { 1.0, 2.0 };

        double[] firstResult = first.Forward(input);
        double[] secondResult = second.Forward(input);

        Assert.Equal(firstResult, secondResult);
    }

    [Fact]
    public void Forward_WithDifferentSeeds_ShouldReturnDifferentResult()
    {
        var first = new Mlp(new[] { 2, 4, 3 }, 42);
        var second = new Mlp(new[] { 2, 4, 3 }, 100);

        double[] input = { 1.0, 2.0 };

        double[] firstResult = first.Forward(input);
        double[] secondResult = second.Forward(input);

        bool hasDifference = false;

        for (int i = 0; i < firstResult.Length; i++)
        {
            if (Math.Abs(firstResult[i] - secondResult[i]) > 1e-12)
            {
                hasDifference = true;
                break;
            }
        }

        Assert.True(hasDifference);
    }

    [Fact]
    public void Forward_ShouldReturnThreeProbabilities()
    {
        var mlp = new Mlp(new[] { 2, 4, 3 }, 42);

        double[] result = mlp.Forward(new[] { 1.0, 2.0 });

        Assert.Equal(3, result.Length);

        for (int i = 0; i < result.Length; i++)
        {
            Assert.InRange(result[i], 0.0, 1.0);
        }
    }

    [Fact]
    public void Forward_WithWrongInputLength_ShouldThrowArgumentException()
    {
        var mlp = new Mlp(new[] { 2, 4, 3 }, 42);

        Assert.Throws<ArgumentException>(
            () => mlp.Forward(new[] { 1.0 }));
    }
}