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
    [Fact]
    public void ChooseCopies_ManualExample_ReturnsCorrectResult()
    {
        int[] weights = { 2, 3 };
        int[][] utilities = {
            new int[] { 0, 5, 7, 8 },
            new int[] { 0, 6, 9, 10 }
        };
        int budget = 7;

        int[] result = Knapsack.ChooseCopies(weights, utilities, budget);

        Assert.Equal(new int[] { 2, 1 }, result);
    }

    [Fact]
    public void ChooseCopies_ZeroBudget_ReturnsAllZeros()
    {
        int[] weights = { 2, 3 };
        int[][] utilities = {
            new int[] { 0, 5, 7, 8 },
            new int[] { 0, 6, 9, 10 }
        };
        
        int[] result = Knapsack.ChooseCopies(weights, utilities, 0);
        
        Assert.Equal(new int[] { 0, 0 }, result);
    }

    [Fact]
    public void ChooseCopies_InvalidInput_ThrowsArgumentException()
    {
        // Різні довжини
        Assert.Throws<ArgumentException>(() => Knapsack.ChooseCopies(new int[] { 1 }, new int[][] { new int[] { 0, 1, 2, 3 }, new int[] { 0, 1, 2, 3 } }, 10));
        // Utilities.Length != 4
        Assert.Throws<ArgumentException>(() => Knapsack.ChooseCopies(new int[] { 1 }, new int[][] { new int[] { 0, 1 } }, 10));
        // Від'ємна вага
        Assert.Throws<ArgumentException>(() => Knapsack.ChooseCopies(new int[] { -2 }, new int[][] { new int[] { 0, 1, 2, 3 } }, 10));
    }

    [Fact]
    public void ChooseCopies_RandomCases_MatchesBruteForce()
    {
        var rnd = new Random(42);
        
        for (int test = 0; test < 20; test++)
        {
            int n = rnd.Next(1, 7); // n <= 6
            int[] weights = new int[n];
            int[][] utilities = new int[n][];
            
            for (int i = 0; i < n; i++)
            {
                weights[i] = rnd.Next(1, 10);
                utilities[i] = new int[] { 0, rnd.Next(1, 5), rnd.Next(5, 10), rnd.Next(10, 15) };
            }
            int budget = rnd.Next(5, 30);

            int[] dpResult = Knapsack.ChooseCopies(weights, utilities, budget);

            int dpUtility = 0;
            int dpWeight = 0;
            for (int i = 0; i < n; i++)
            {
                dpUtility += utilities[i][dpResult[i]];
                dpWeight += dpResult[i] * weights[i];
            }

            int bruteForceMaxUtility = BruteForceMaxUtility(weights, utilities, budget);

            Assert.True(dpWeight <= budget);
            Assert.Equal(bruteForceMaxUtility, dpUtility);
        }
    }

    private int BruteForceMaxUtility(int[] weights, int[][] utilities, int budget)
    {
        int maxUtility = 0;
        int n = weights.Length;

        void Solve(int item, int currentWeight, int currentUtility)
        {
            if (item == n)
            {
                if (currentUtility > maxUtility) maxUtility = currentUtility;
                return;
            }

            for (int r = 0; r <= 3; r++)
            {
                int cost = r * weights[item];
                if (currentWeight + cost <= budget)
                {
                    Solve(item + 1, currentWeight + cost, currentUtility + utilities[item][r]);
                }
            }
        }

        Solve(0, 0, 0);
        return maxUtility;
}