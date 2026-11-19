namespace LogKeeper.Security;

/// <summary>Найкращий знайдений розв'язок. History[k] — найкращий fitness після k+1 обчислень fitness.</summary>
public sealed record GaResult(double[] BestGenes, double BestFitness)
{
    public double[] History { get; init; } = Array.Empty<double>();
    public int Evaluations => History.Length;
}

/// <summary>Параметри ГА (D-006).</summary>
public sealed record GaOptions(
    int TournamentSize = 3,
    double CrossoverRate = 0.9,
    double MutationRate = 0.1,
    double MutationSigma = 0.1,
    int EliteCount = 2)
{
    public static GaOptions Default { get; } = new();
}

/// <summary>
/// Генетичний алгоритм на векторах генів у [0, 1): турнірна селекція, рівномірний кросовер,
/// гаусова мутація (з рідким випадковим перезапуском гена), елітизм, фіксований seed.
/// Fitness максимізується. Для порівняння — випадковий пошук з тим самим бюджетом обчислень.
/// </summary>
public static class GeneticAlgorithm
{
    public static GaResult Run(Func<double[], double> fitness, int geneCount, int populationSize, int generations,
        int seed, GaOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(fitness);
        if (geneCount <= 0) throw new ArgumentOutOfRangeException(nameof(geneCount));
        if (populationSize < 2) throw new ArgumentOutOfRangeException(nameof(populationSize));
        if (generations < 0) throw new ArgumentOutOfRangeException(nameof(generations));
        var o = options ?? GaOptions.Default;
        int elite = Math.Clamp(o.EliteCount, 0, populationSize - 1);

        var rng = new Random(seed);
        var history = new List<double>();
        double best = double.NegativeInfinity;
        double[] bestGenes = Array.Empty<double>();

        double Evaluate(double[] genes)
        {
            double f = fitness(genes);
            if (double.IsNaN(f)) f = double.NegativeInfinity;
            if (f > best)
            {
                best = f;
                bestGenes = (double[])genes.Clone();
            }
            history.Add(best);
            return f;
        }

        var population = new double[populationSize][];
        var scores = new double[populationSize];
        for (int i = 0; i < populationSize; i++)
        {
            population[i] = RandomGenes(rng, geneCount);
            scores[i] = Evaluate(population[i]);
        }

        for (int g = 0; g < generations; g++)
        {
            var next = new double[populationSize][];
            var nextScores = new double[populationSize];

            // Елітизм: найкращі особини переходять без змін (і без повторного обчислення fitness).
            int[] ranked = Enumerable.Range(0, populationSize).OrderByDescending(i => scores[i]).ToArray();
            for (int e = 0; e < elite; e++)
            {
                next[e] = population[ranked[e]];
                nextScores[e] = scores[ranked[e]];
            }

            for (int k = elite; k < populationSize; k++)
            {
                double[] a = population[Tournament(rng, scores, o.TournamentSize)];
                double[] b = population[Tournament(rng, scores, o.TournamentSize)];
                double[] child = rng.NextDouble() < o.CrossoverRate ? Crossover(rng, a, b) : (double[])a.Clone();
                Mutate(rng, child, o.MutationRate, o.MutationSigma);
                next[k] = child;
                nextScores[k] = Evaluate(child);
            }

            population = next;
            scores = nextScores;
        }

        return new GaResult(bestGenes, best) { History = history.ToArray() };
    }

    /// <summary>Випадковий пошук: <paramref name="evaluations"/> незалежних випадкових векторів.</summary>
    public static GaResult RandomSearch(Func<double[], double> fitness, int geneCount, int evaluations, int seed)
    {
        ArgumentNullException.ThrowIfNull(fitness);
        if (geneCount <= 0) throw new ArgumentOutOfRangeException(nameof(geneCount));
        if (evaluations <= 0) throw new ArgumentOutOfRangeException(nameof(evaluations));

        var rng = new Random(seed);
        var history = new double[evaluations];
        double best = double.NegativeInfinity;
        double[] bestGenes = Array.Empty<double>();
        for (int i = 0; i < evaluations; i++)
        {
            var genes = RandomGenes(rng, geneCount);
            double f = fitness(genes);
            if (f > best)
            {
                best = f;
                bestGenes = genes;
            }
            history[i] = best;
        }
        return new GaResult(bestGenes, best) { History = history };
    }

    /// <summary>Кількість обчислень fitness, яку виконає <see cref="Run"/>.</summary>
    public static int EvaluationsFor(int populationSize, int generations, GaOptions? options = null)
    {
        int elite = Math.Clamp((options ?? GaOptions.Default).EliteCount, 0, populationSize - 1);
        return populationSize + generations * (populationSize - elite);
    }

    private static double[] RandomGenes(Random rng, int n)
    {
        var g = new double[n];
        for (int i = 0; i < n; i++) g[i] = rng.NextDouble();
        return g;
    }

    private static int Tournament(Random rng, double[] scores, int size)
    {
        int best = rng.Next(scores.Length);
        for (int i = 1; i < size; i++)
        {
            int c = rng.Next(scores.Length);
            if (scores[c] > scores[best]) best = c;
        }
        return best;
    }

    private static double[] Crossover(Random rng, double[] a, double[] b)
    {
        var child = new double[a.Length];
        for (int i = 0; i < a.Length; i++)
            child[i] = rng.NextDouble() < 0.5 ? a[i] : b[i];
        return child;
    }

    private static void Mutate(Random rng, double[] genes, double rate, double sigma)
    {
        for (int i = 0; i < genes.Length; i++)
        {
            if (rng.NextDouble() >= rate) continue;
            if (rng.NextDouble() < 0.2)
            {
                genes[i] = rng.NextDouble(); // рідкий перезапуск гена — вихід з локального максимуму
                continue;
            }
            double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
            double gauss = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
            genes[i] = Math.Clamp(genes[i] + sigma * gauss, 0, Math.BitDecrement(1.0));
        }
    }
}
