using System.Text;
using LogKeeper.Security;

namespace LogKeeper.Tests.Security;

public class GeneticAlgorithmTests
{
    // Максимум 0 у точці (0.3, 0.7, 0.5).
    private static double Peak(double[] g) =>
        -((g[0] - 0.3) * (g[0] - 0.3) + (g[1] - 0.7) * (g[1] - 0.7) + (g[2] - 0.5) * (g[2] - 0.5));

    [Fact]
    public void Run_FindsMaximumOfSimpleFunction()
    {
        var r = GeneticAlgorithm.Run(Peak, 3, 40, 60, seed: 1);

        Assert.True(r.BestFitness > -1e-3, $"fitness = {r.BestFitness}");
        Assert.Equal(0.3, r.BestGenes[0], 1);
        Assert.Equal(0.7, r.BestGenes[1], 1);
        Assert.Equal(0.5, r.BestGenes[2], 1);
    }

    [Fact]
    public void Run_SameSeed_SameResult()
    {
        var a = GeneticAlgorithm.Run(Peak, 3, 20, 10, seed: 7);
        var b = GeneticAlgorithm.Run(Peak, 3, 20, 10, seed: 7);

        Assert.Equal(a.BestGenes, b.BestGenes);
        Assert.Equal(a.History, b.History);
    }

    [Fact]
    public void Run_HistoryIsMonotone_AndCountsEvaluations()
    {
        var r = GeneticAlgorithm.Run(Peak, 3, 20, 10, seed: 3);

        Assert.Equal(GeneticAlgorithm.EvaluationsFor(20, 10), r.Evaluations);
        for (int i = 1; i < r.History.Length; i++)
            Assert.True(r.History[i] >= r.History[i - 1]);
        Assert.Equal(r.BestFitness, r.History[^1]);
    }

    [Fact]
    public void Run_BeatsRandomSearch_WithSameNumberOfEvaluations()
    {
        // 10-вимірна задача: випадковий пошук тут помітно слабший.
        static double Sphere(double[] g) => -g.Sum(x => (x - 0.42) * (x - 0.42));
        var ga = GeneticAlgorithm.Run(Sphere, 10, 30, 40, seed: 5);
        var rs = GeneticAlgorithm.RandomSearch(Sphere, 10, ga.Evaluations, seed: 5);

        Assert.True(ga.BestFitness > rs.BestFitness, $"GA {ga.BestFitness} vs RS {rs.BestFitness}");
    }

    [Fact]
    public void WeakCipher_IsInvolution()
    {
        byte[] data = Encoding.ASCII.GetBytes("hello replication");
        byte[] key = { 1, 2, 3 };
        Assert.Equal(data, WeakCipher.Crypt(WeakCipher.Crypt(data, key), key));
    }

    [Fact]
    public void Attack_RecoversPlaintext_ForKnownKey()
    {
        var sb = new StringBuilder();
        var rng = new Random(9);
        string[] msgs =
        {
            "replication batch written to node", "packet rejected by integrity check",
            "node heartbeat received from cluster", "metric value exceeded the warning threshold",
            "recovery finished without errors", "critical event stored in three copies",
        };
        for (int i = 0; i < 25; i++)
            sb.Append($"2026-11-19 10:{i:D2}:0{i % 10} INFO source={rng.Next(1, 9)} {msgs[rng.Next(msgs.Length)]}\n");
        byte[] plaintext = Encoding.ASCII.GetBytes(sb.ToString());
        byte[] key = Encoding.ASCII.GetBytes("K3y!x9Qz");

        byte[] recovered = WeakCipherAttack.Recover(WeakCipher.Crypt(plaintext, key), seed: 1);

        Assert.Equal(Encoding.ASCII.GetString(plaintext), Encoding.ASCII.GetString(recovered));
    }
}
