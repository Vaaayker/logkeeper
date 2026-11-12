using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using LogKeeper.Scheduler;

namespace LogKeeper.Tests.Scheduler;

public class GreedyAndPlacementTests
{
    [Fact]
    public void GreedyChooseCopies_NeverExceedsBudget()
    {
        int[] weights = { 2, 3, 5 };
        int[][] utilities = {
            new int[] { 0, 5, 8, 10 },
            new int[] { 0, 6, 11, 14 },
            new int[] { 0, 10, 15, 20 }
        };
        int budget = 7;

        int[] result = GreedyScheduler.ChooseCopies(weights, utilities, budget);

        int totalWeight = result.Select((c, i) => c * weights[i]).Sum();
        Assert.True(totalWeight <= budget);
    }

    [Fact]
    public void GreedyChooseCopies_ComparedToDP()
    {
        // Випадок 1: Жадібний знаходить оптимальне рішення (як ДП)
        int[] weights1 = { 2, 3 };
        int[][] utils1 = {
            new int[] { 0, 10, 20, 30 },
            new int[] { 0, 5, 10, 15 }
        };
        int budget1 = 5;

        var greedy1 = GreedyScheduler.ChooseCopies(weights1, utils1, budget1);
        var dp1 = Knapsack.ChooseCopies(weights1, utils1, budget1);
        Assert.Equal(CalcUtility(utils1, dp1), CalcUtility(utils1, greedy1));

        // Випадок 2: Жадібний СТРОГО ГІРШИЙ за ДП
        // Жадібний візьме копію події 0 (питома цінність 13/6 = 2.16) і заповнить рюкзак до 6 (залишиться 4).
        // ДП зрозуміє, що краще взяти дві копії події 1 (питома цінність 10/5 = 2.0, але сумарно дає 20).
        int[] weights2 = { 6, 5 };
        int[][] utils2 = {
            new int[] { 0, 13, 20, 20 },
            new int[] { 0, 10, 20, 20 }
        };
        int budget2 = 10;

        var greedy2 = GreedyScheduler.ChooseCopies(weights2, utils2, budget2);
        var dp2 = Knapsack.ChooseCopies(weights2, utils2, budget2);

        int greedyVal = CalcUtility(utils2, greedy2); // Отримає 13
        int dpVal = CalcUtility(utils2, dp2); // Отримає 20

        Assert.True(greedyVal < dpVal);
    }

    [Fact]
    public void PlanBatches_PlacesCopiesOnDistinctNodes()
    {
        int[] copies = { 2, 1, 3 };
        var nodes = new List<string> { "nodeA", "nodeB", "nodeC", "nodeD" };

        var batches = Placement.PlanBatches(copies, nodes);

        // Перевіряємо, що подія 0 (2 копії) знаходиться рівно на 2 унікальних вузлах
        var nodesWithEvent0 = batches.Where(kvp => kvp.Value.Contains(0)).Select(kvp => kvp.Key).ToList();
        Assert.Equal(2, nodesWithEvent0.Count);
        Assert.Distinct(nodesWithEvent0);

        // Перевіряємо, що подія 2 (3 копії) знаходиться рівно на 3 унікальних вузлах
        var nodesWithEvent2 = batches.Where(kvp => kvp.Value.Contains(2)).Select(kvp => kvp.Key).ToList();
        Assert.Equal(3, nodesWithEvent2.Count);
        Assert.Distinct(nodesWithEvent2);
    }

    [Fact]
    public void PlanBatches_ThrowsIfCopiesExceedNodes()
    {
        int[] copies = { 4 };
        var nodes = new List<string> { "nodeA", "nodeB", "nodeC" };

        Assert.Throws<ArgumentException>(() => Placement.PlanBatches(copies, nodes));
    }

    private int CalcUtility(int[][] utils, int[] copies)
    {
        return copies.Select((c, i) => utils[i][c]).Sum();
    }
}