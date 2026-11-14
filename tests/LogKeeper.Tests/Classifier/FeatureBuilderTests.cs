
using System;
using LogKeeper.Classifier;
using LogKeeper.Core;
using Xunit;

namespace LogKeeper.Tests.Classifier;

public class FeatureBuilderTests
{
    // Порожнє вікно повинно повертати 14 нулів.
    [Fact]
    public void Build_EmptyWindow_ReturnsZeros()
    {
        LogRecord[] window = Array.Empty<LogRecord>();

        double[] result = FeatureBuilder.Build(window);

        Assert.Equal(FeatureBuilder.FeatureCount, result.Length);
        Assert.Equal(new double[14], result);
    }

    // Перевіряємо правильність усіх основних ознак.
    [Fact]
    public void Build_ThreeRecords_ReturnsCorrectFeatures()
    {
        LogRecord[] window =
        {
            new LogRecord(1000, 1, 10, 10, 1, 2, false, 0),
            new LogRecord(2000, 1, 10, 15, 2, 4, true, 0),
            new LogRecord(3000, 1, 10, 20, 1, 3, false, 0)
        };

        double[] result = FeatureBuilder.Build(window);

        Assert.Equal(14, result.Length);

        // Загальні ознаки.
        Assert.Equal(3.0, result[0]);
        Assert.Equal(10.0, result[1]);
        Assert.Equal(3.0, result[2]);
        Assert.Equal(1.0 / 3.0, result[3], 10);

        // Лічильники категорій.
        Assert.Equal(0.0, result[4]);  // Категорія 0
        Assert.Equal(2.0, result[5]);  // Категорія 1
        Assert.Equal(1.0, result[6]);  // Категорія 2

        // Решта категорій відсутні.
        for (int i = 7; i < result.Length; i++)
        {
            Assert.Equal(0.0, result[i]);
        }
    }

    // Один запис повинен повертати 14 ознак.
    [Fact]
    public void Build_SingleRecord_ReturnsCorrectFeatures()
    {
        LogRecord[] window =
        {
            new LogRecord(1000, 1, 10, 50, 2, 4, true, 0)
        };

        double[] result = FeatureBuilder.Build(window);

        Assert.Equal(14, result.Length);
        Assert.Equal(1.0, result[0]);
        Assert.Equal(0.0, result[1]);
        Assert.Equal(4.0, result[2]);
        Assert.Equal(1.0, result[3]);
        Assert.Equal(1.0, result[6]); // Категорія 2
    }

    // Перевіряємо крайні категорії: 0 і 9.
    [Fact]
    public void Build_CategoryZeroAndNine_CountsCorrectly()
    {
        LogRecord[] window =
        {
            new LogRecord(1000, 1, 10, 10, 0, 1, false, 0),
            new LogRecord(2000, 1, 10, 20, 9, 2, false, 0),
            new LogRecord(3000, 1, 10, 30, 9, 3, true, 0)
        };

        double[] result = FeatureBuilder.Build(window);

        Assert.Equal(14, result.Length);
        Assert.Equal(1.0, result[4]);  // Категорія 0
        Assert.Equal(2.0, result[13]); // Категорія 9
    }

    // Однакові дані повинні давати однаковий результат.
    [Fact]
    public void Build_SameWindow_ReturnsSameResult()
    {
        LogRecord[] window =
        {
            new LogRecord(1000, 1, 10, 10, 1, 2, false, 0),
            new LogRecord(2000, 1, 10, 20, 2, 3, true, 0)
        };

        double[] first = FeatureBuilder.Build(window);
        double[] second = FeatureBuilder.Build(window);

        Assert.Equal(first, second);
    }

    // Null повинен викликати виняток.
    [Fact]
    public void Build_NullWindow_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(
            () => FeatureBuilder.Build(null!));
    }

    // Категорії поза діапазоном 0-9 заборонені.
    [Fact]
    public void Build_InvalidCategory_ThrowsException()
    {
        LogRecord[] window =
        {
            new LogRecord(1000, 1, 10, 10, 10, 2, false, 0)
        };

        Assert.Throws<ArgumentException>(
            () => FeatureBuilder.Build(window));
    }
}
