using System;
using System.Collections.Generic;

namespace LogKeeper.Scheduler;

/// <summary>Динамічне програмування: рюкзак 0/1, далі багатоваріантний (0/1/2/3 копії).</summary>
public static class Knapsack
{
    /// <summary>Повертає (найбільша цінність, індекси вибраних предметів).</summary>
    public static (int BestValue, List<int> Chosen) Solve01(int[] weights, int[] values, int capacity)
    {
        if (weights == null || values == null || weights.Length == 0 || capacity <= 0)
        {
            return (0, new List<int>());
        }

        int n = weights.Length;
        int[,] dp = new int[n + 1, capacity + 1];

        for (int i = 1; i <= n; i++)
        {
            int currentWeight = weights[i - 1];
            int currentValue = values[i - 1];

            for (int w = 0; w <= capacity; w++)
            {
                if (currentWeight <= w)
                {
                    dp[i, w] = Math.Max(dp[i - 1, w], dp[i - 1, w - currentWeight] + currentValue);
                }
                else
                {
                    dp[i, w] = dp[i - 1, w];
                }
            }
        }

        int res = dp[n, capacity];
        int wCurrent = capacity;
        List<int> chosen = new List<int>();

        for (int i = n; i > 0 && res > 0; i--)
        {
            if (res != dp[i - 1, wCurrent])
            {
                chosen.Add(i - 1);
                res -= values[i - 1];
                wCurrent -= weights[i - 1];
            }
        }

        chosen.Reverse();
        return (dp[n, capacity], chosen);
    }

    /// <summary>Багатоваріантний рюкзак: кількість копій r_i для кожної події.</summary>
    public static int[] ChooseCopies(int[] weights, int[][] utilities, int budget)
        => throw new NotImplementedException();
}