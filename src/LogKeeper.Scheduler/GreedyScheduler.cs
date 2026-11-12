using System;

namespace LogKeeper.Scheduler;

/// <summary>Жадібний baseline: додаємо копію з найбільшим приростом корисності на одиницю вартості.</summary>
public static class GreedyScheduler
{
    public static int[] ChooseCopies(int[] weights, int[][] utilities, int budget)
    {
        if (weights == null || utilities == null || weights.Length == 0) 
            return Array.Empty<int>();
        
        if (budget <= 0) 
            return new int[weights.Length];

        int n = weights.Length;
        int[] copies = new int[n];
        int currentWeight = 0;

        while (true)
        {
            int bestItem = -1;
            double bestRatio = -1.0;
            
            for (int i = 0; i < n; i++)
            {
                int c = copies[i];
                if (c < 3 && currentWeight + weights[i] <= budget)
                {
                    int gain = utilities[i][c + 1] - utilities[i][c];
                    if (gain > 0)
                    {
                        double ratio = weights[i] == 0 ? double.PositiveInfinity : (double)gain / weights[i];
                        if (ratio > bestRatio)
                        {
                            bestRatio = ratio;
                            bestItem = i;
                        }
                    }
                }
            }

            if (bestItem == -1) break; // Не залишилося вигідних копій, що вміщуються у бюджет

            copies[bestItem]++;
            currentWeight += weights[bestItem];
        }

        return copies;
    }
}