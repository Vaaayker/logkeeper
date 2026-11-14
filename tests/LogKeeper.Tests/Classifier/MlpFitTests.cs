
using System;
using LogKeeper.Classifier;
using Xunit;

namespace LogKeeper.Tests.Classifier;

public class MlpFitTests
{
    // Створюємо навчальний набір із трьома класами.
    private static void CreateDataset(out double[][] x, out int[] y)
    {
        x = new double[90][];
        y = new int[90];

        for (int i = 0; i < 90; i++)
        {
            int classId = i / 30;
            double variation = (i % 10) * 0.01;

            x[i] = new double[5];

            // Кожен клас має свою характерну ознаку.
            x[i][classId] = 2.0 + variation;
            x[i][3] = 0.1 + variation;
            x[i][4] = 0.2;

            y[i] = classId;
        }
    }

    // Обчислюємо середню cross-entropy вручну.
    private static double CalculateLoss(Mlp model, double[][] x, int[] y)
    {
        double totalLoss = 0.0;

        for (int i = 0; i < x.Length; i++)
        {
            double[] probabilities = model.Forward(x[i]);

            // Захист від log(0).
            double probability = Math.Max(probabilities[y[i]], 1e-12);

            totalLoss -= Math.Log(probability);
        }

        return totalLoss / x.Length;
    }

    // Після навчання помилка повинна зменшитися.
    [Fact]
    public void Fit_ReducesLoss()
    {
        CreateDataset(out double[][] x, out int[] y);

        Mlp model = new Mlp(new[] { 5, 10, 3 }, seed: 42);

        double lossBefore = CalculateLoss(model, x, y);

        model.Fit(x, y, epochs: 200, learningRate: 0.01);

        double lossAfter = CalculateLoss(model, x, y);

        Assert.True(
            lossAfter < lossBefore,
            $"Loss до навчання: {lossBefore}, після: {lossAfter}");
    }

    // Точність на синтетичному наборі повинна бути не менше 90%.
    [Fact]
    public void Fit_ThreeClasses_AccuracyAtLeast90Percent()
    {
        CreateDataset(out double[][] x, out int[] y);

        Mlp model = new Mlp(new[] { 5, 10, 3 }, seed: 42);

        model.Fit(x, y, epochs: 200, learningRate: 0.01);

        int[] predictions = model.Predict(x);

        int correct = 0;

        for (int i = 0; i < y.Length; i++)
        {
            if (predictions[i] == y[i])
                correct++;
        }

        double accuracy = (double)correct / y.Length;

        Assert.True(
            accuracy >= 0.9,
            $"Точність: {accuracy:P2}. Очікується не менше 90%.");
    }

    // Однаковий seed і однакові дані дають однаковий результат.
    [Fact]
    public void Fit_SameSeed_ProducesSameResult()
    {
        CreateDataset(out double[][] x, out int[] y);

        Mlp first = new Mlp(new[] { 5, 10, 3 }, seed: 42);
        Mlp second = new Mlp(new[] { 5, 10, 3 }, seed: 42);

        first.Fit(x, y, epochs: 100, learningRate: 0.01);
        second.Fit(x, y, epochs: 100, learningRate: 0.01);

        double[] firstResult = first.Forward(x[0]);
        double[] secondResult = second.Forward(x[0]);

        Assert.Equal(firstResult, secondResult);
    }
}
