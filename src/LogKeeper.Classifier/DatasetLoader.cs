namespace LogKeeper.Classifier;

/// <summary>Завантаження train.csv / valid.csv.</summary>
public static class DatasetLoader
{
    /// <summary>Читає CSV, повертає рядки як словники {назва колонки: значення} (назви — як у файлі).</summary>
    public static List<Dictionary<string, string>> LoadEvents(string path)
        => throw new NotImplementedException();

    /// <summary>Скільки записів кожного класу (0/1/2).</summary>
    public static Dictionary<int, int> ClassCounts(IReadOnlyList<Dictionary<string, string>> events)
        => throw new NotImplementedException();
}
