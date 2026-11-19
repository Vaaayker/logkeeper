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

    /// <summary>Найбільша кількість копій однієї події.</summary>
    public const int MaxCopies = 3;

    /// <summary>
    /// Багатоваріантний рюкзак: для кожної події i обирається r_i ∈ {0,1,2,3}
    /// з корисністю utilities[i][r_i] і вагою weights[i] * r_i так, щоб
    /// Σ weights[i] * r_i ≤ budget, а Σ utilities[i][r_i] була максимальною.
    /// dp[i][b] — найкраща корисність перших i подій при вазі ≤ b;
    /// choice[i][b] — r, на якому досягнуто dp[i][b] (для відновлення вибору).
    /// Час O(n · budget · 4), пам'ять O(n · budget) байтів.
    /// </summary>
    public static int[] ChooseCopies(int[] weights, int[][] utilities, int budget)
    {
        if (weights == null || utilities == null || weights.Length == 0)
            return Array.Empty<int>();
        if (utilities.Length != weights.Length)
            throw new ArgumentException("Кількість рядків utilities має збігатися з кількістю ваг.");

        int n = weights.Length;
        if (budget <= 0)
            return ZeroWeightOnly(weights, utilities);

        for (int i = 0; i < n; i++)
        {
            if (weights[i] < 0)
                throw new ArgumentException($"Вага події {i} від'ємна.");
            if (utilities[i] == null || utilities[i].Length < MaxCopies + 1)
                throw new ArgumentException($"utilities[{i}] має містити {MaxCopies + 1} значення (r = 0..{MaxCopies}).");
        }

        long[] prev = new long[budget + 1];
        long[] cur = new long[budget + 1];
        var choice = new byte[n][];

        for (int i = 0; i < n; i++)
        {
            int w = weights[i];
            int[] u = utilities[i];
            var ch = new byte[budget + 1];

            for (int b = 0; b <= budget; b++)
            {
                long best = prev[b] + u[0];
                byte bestR = 0;
                for (int r = 1; r <= MaxCopies; r++)
                {
                    long cost = (long)w * r;
                    if (cost > b) break;
                    long candidate = prev[b - cost] + u[r];
                    if (candidate > best)
                    {
                        best = candidate;
                        bestR = (byte)r;
                    }
                }
                cur[b] = best;
                ch[b] = bestR;
            }

            choice[i] = ch;
            (prev, cur) = (cur, prev);
        }

        // Відновлення: йдемо від останньої події, віднімаючи вагу обраних копій.
        var copies = new int[n];
        int rest = budget;
        for (int i = n - 1; i >= 0; i--)
        {
            int r = choice[i][rest];
            copies[i] = r;
            rest -= weights[i] * r;
        }
        return copies;
    }

    // При нульовому бюджеті можна брати лише копії нульової ваги.
    private static int[] ZeroWeightOnly(int[] weights, int[][] utilities)
    {
        var copies = new int[weights.Length];
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] != 0 || utilities[i] == null) continue;
            int best = 0;
            for (int r = 1; r <= MaxCopies && r < utilities[i].Length; r++)
                if (utilities[i][r] > utilities[i][best]) best = r;
            copies[i] = best;
        }
        return copies;
    }
}