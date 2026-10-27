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
    // Складність: O(n · W · 4) за часом, O(n · W) за пам'яттю
    public static int[] ChooseCopies(int[] weights, int[][] utilities, int budget)
    {
        if (weights == null || utilities == null || weights.Length == 0)
            return Array.Empty<int>();

        if (budget <= 0)
            return new int[weights.Length];

        if (weights.Length != utilities.Length)
            throw new ArgumentException("Довжини масивів weights та utilities не збігаються.");

        for (int i = 0; i < weights.Length; i++)
        {
            if (utilities[i] == null || utilities[i].Length != 4)
                throw new ArgumentException($"Масив utilities[{i}] має містити рівно 4 елементи.");
            if (weights[i] < 0)
                throw new ArgumentException($"Вага weights[{i}] не може бути від'ємною.");
        }

        int n = weights.Length;
        int[,] dp = new int[n + 1, budget + 1];

        // Прямий прохід
        for (int i = 1; i <= n; i++)
        {
            int wItem = weights[i - 1];
            for (int w = 0; w <= budget; w++)
            {
                dp[i, w] = dp[i - 1, w]; // Базовий вибір: r = 0

                for (int r = 1; r <= 3; r++)
                {
                    int cost = r * wItem;
                    if (cost <= w)
                    {
                        dp[i, w] = Math.Max(dp[i, w], dp[i - 1, w - cost] + utilities[i - 1][r]);
                    }
                }
            }
        }

        // Зворотний прохід (відновлення відповіді)
        int[] chosenCopies = new int[n];
        int currentW = budget;

        for (int i = n; i >= 1; i--)
        {
            int wItem = weights[i - 1];
            for (int r = 0; r <= 3; r++)
            {
                int cost = r * wItem;
                if (currentW >= cost && dp[i, currentW] == dp[i - 1, currentW - cost] + utilities[i - 1][r])
                {
                    chosenCopies[i - 1] = r;
                    currentW -= cost;
                    break;
                }
            }
        }

        return chosenCopies;
    }
}