namespace LogKeeper.Scheduler;

/// <summary>Динамічне програмування: рюкзак 0/1, далі багатоваріантний (0/1/2/3 копії).</summary>
public static class Knapsack
{
    /// <summary>Повертає (найбільша цінність, індекси вибраних предметів).</summary>
    public static (int BestValue, List<int> Chosen) Solve01(int[] weights, int[] values, int capacity)
        => throw new NotImplementedException();

    /// <summary>Багатоваріантний рюкзак: кількість копій r_i для кожної події.</summary>
    public static int[] ChooseCopies(int[] weights, int[][] utilities, int budget)
        => throw new NotImplementedException();
}
