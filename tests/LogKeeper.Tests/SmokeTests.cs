using LogKeeper.Classifier;
using LogKeeper.Compression;
using LogKeeper.Core;
using LogKeeper.Integration;
using LogKeeper.Scheduler;
using LogKeeper.Security;

namespace LogKeeper.Tests;

/// <summary>Мінімальна перевірка: усі модулі підключені, `dotnet test` працює з першого коміту.</summary>
public class SmokeTests
{
    [Fact]
    public void AllModulesAreReferenced()
    {
        Assert.NotNull(typeof(RecordCodec));
        Assert.NotNull(typeof(Huffman));
        Assert.NotNull(typeof(Knapsack));
        Assert.NotNull(typeof(DatasetLoader));
        Assert.NotNull(typeof(KeyDerivation));
        Assert.NotNull(typeof(Pipeline));
    }
}
