using Xunit;
using System.Collections.Generic;
using System.Linq;
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

    // ---------- ChooseCopies (багатоваріантний рюкзак) ----------

    private static int Utility(int[][] u, int[] copies) => copies.Select((c, i) => u[i][c]).Sum();
    private static int Weight(int[] w, int[] copies) => copies.Select((c, i) => c * w[i]).Sum();

    [Fact]
    public void ChooseCopies_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(Knapsack.ChooseCopies(System.Array.Empty<int>(), System.Array.Empty<int[]>(), 10));
    }

    [Fact]
    public void ChooseCopies_ZeroBudget_AllZero()
    {
        int[] w = { 2, 3 };
        int[][] u = { new[] { 0, 5, 8, 9 }, new[] { 0, 4, 6, 7 } };

        Assert.Equal(new[] { 0, 0 }, Knapsack.ChooseCopies(w, u, 0));
    }

    [Fact]
    public void ChooseCopies_SmallExample_MatchesHandSolution()
    {
        // Бюджет 6. Варіанти (перевірено вручну):
        //   r = (3,0): вага 6, корисність 12
        //   r = (1,1): вага 5, корисність 6 + 9 = 15
        //   r = (2,1): вага 7 — не вміщується
        //   r = (0,2): вага 6, корисність 14
        //   r = (1,1) — оптимум 15.
        int[] w = { 2, 3 };
        int[][] u = { new[] { 0, 6, 10, 12 }, new[] { 0, 9, 14, 16 } };

        int[] copies = Knapsack.ChooseCopies(w, u, 6);

        Assert.Equal(new[] { 1, 1 }, copies);
        Assert.Equal(15, Utility(u, copies));
    }

    [Fact]
    public void ChooseCopies_LargeBudget_TakesAllThreeCopies()
    {
        int[] w = { 1, 2, 3 };
        int[][] u = { new[] { 0, 1, 2, 3 }, new[] { 0, 1, 2, 3 }, new[] { 0, 1, 2, 3 } };

        Assert.Equal(new[] { 3, 3, 3 }, Knapsack.ChooseCopies(w, u, 100));
    }

    [Fact]
    public void ChooseCopies_MatchesBruteForce_AndBeatsGreedy_OnRandomData()
    {
        var rng = new System.Random(42);
        for (int trial = 0; trial < 200; trial++)
        {
            int n = rng.Next(1, 7);
            int[] w = new int[n];
            int[][] u = new int[n][];
            for (int i = 0; i < n; i++)
            {
                w[i] = rng.Next(1, 8);
                int a = rng.Next(0, 20), b = rng.Next(0, 15), c = rng.Next(0, 10);
                u[i] = new[] { 0, a, a + b, a + b + c };
            }
            int budget = rng.Next(0, 30);

            int[] dp = Knapsack.ChooseCopies(w, u, budget);
            int[] greedy = GreedyScheduler.ChooseCopies(w, u, budget);

            Assert.True(Weight(w, dp) <= budget);
            Assert.Equal(BruteForce(w, u, budget), Utility(u, dp));
            Assert.True(Utility(u, dp) >= Utility(u, greedy));
        }
    }

    private static int BruteForce(int[] w, int[][] u, int budget)
    {
        int n = w.Length, best = 0;
        int total = (int)System.Math.Pow(4, n);
        for (int mask = 0; mask < total; mask++)
        {
            int m = mask, weight = 0, value = 0;
            for (int i = 0; i < n; i++, m /= 4)
            {
                weight += w[i] * (m % 4);
                value += u[i][m % 4];
            }
            if (weight <= budget && value > best) best = value;
        }
        return best;
    }
}
