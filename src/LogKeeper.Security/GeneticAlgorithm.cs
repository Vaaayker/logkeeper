namespace LogKeeper.Security;

public sealed record GaResult(double[] BestGenes, double BestFitness);

/// <summary>Генетичний алгоритм: турнірна селекція, кросовер, мутація, елітизм; порівняння з random search.</summary>
public static class GeneticAlgorithm
{
    public static GaResult Run(Func<double[], double> fitness, int populationSize, int generations, int seed)
        => throw new NotImplementedException();
}
