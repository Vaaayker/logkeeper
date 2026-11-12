using System;
using System.Collections.Generic;
using System.Linq;

namespace LogKeeper.Scheduler;

/// <summary>Куди й у якому порядку писати копії (не більше одного batch на вузол у вікні).</summary>
public static class Placement
{
    public static Dictionary<string, List<int>> PlanBatches(int[] copies, IReadOnlyList<string> nodeIds)
    {
        if (copies == null || nodeIds == null || nodeIds.Count == 0)
            return new Dictionary<string, List<int>>();

        var batches = nodeIds.ToDictionary(n => n, n => new List<int>());
        int nodeIndex = 0;

        for (int i = 0; i < copies.Length; i++)
        {
            int r = copies[i];
            if (r > nodeIds.Count)
                throw new ArgumentException($"Неможливо розмістити {r} копій події {i} на {nodeIds.Count} вузлах.");

            // Розподіл Round-Robin гарантує, що r копій (де r <= nodes.Count) потраплять на унікальні вузли
            for (int k = 0; k < r; k++)
            {
                batches[nodeIds[nodeIndex]].Add(i);
                nodeIndex = (nodeIndex + 1) % nodeIds.Count;
            }
        }

        return batches;
    }
}