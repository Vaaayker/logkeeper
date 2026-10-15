namespace LogKeeper.Scheduler;

/// <summary>Куди й у якому порядку писати копії (не більше одного batch на вузол у вікні).</summary>
public static class Placement
{
    public static Dictionary<string, List<int>> PlanBatches(int[] copies, IReadOnlyList<string> nodeIds)
        => throw new NotImplementedException();
}
