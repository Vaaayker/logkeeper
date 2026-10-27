using Xunit;
using System.Collections.Generic;
using LogKeeper.Scheduler;

namespace LogKeeper.Tests.Scheduler;

public class KnapsackTests
{
    [Fact]
    public void Solve01_StandardCase_ReturnsCorrectValueAndItems()
    {
        int[] weights = { 1, 3, 4, 5 };
        int[] values = { 1, 4, 5, 7 };
        int capacity = 7;

        var (bestValue, chosen) = Knapsack.Solve01(weights, values, capacity);

        Assert.Equal(9, bestValue);
        Assert.Equal(new List<int> { 1, 2 }, chosen);
    }

    [Fact]
    public void Solve01_ZeroCapacityAndEmptyArrays_ReturnsZeroAndEmptyList()
    {
        int[] weights = System.Array.Empty<int>();
        int[] values = System.Array.Empty<int>();
        int capacity = 0;

        var (bestValue, chosen) = Knapsack.Solve01(weights, values, capacity);

        Assert.Equal(0, bestValue);
        Assert.Empty(chosen);
    }

    [Fact]
    public void Solve01_ItemExceedsCapacity_IsNotChosen()
    {
        int[] weights = { 2, 10, 3 }; // Предмет з індексом 1 важить 10 і не влазить
        int[] values = { 5, 50, 4 };
        int capacity = 6;

        var (bestValue, chosen) = Knapsack.Solve01(weights, values, capacity);

        Assert.Equal(9, bestValue); 
        Assert.Equal(new List<int> { 0, 2 }, chosen);
        Assert.DoesNotContain(1, chosen);
    }
}