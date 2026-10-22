namespace LogKeeper.Classifier;

/// <summary>Завантаження train.csv / valid.csv.</summary>
public static class DatasetLoader
{
    public static List<Dictionary<string, string>> LoadEvents(string path)
    {
        var events = new List<Dictionary<string, string>>();

        using var reader = new StreamReader(path);

        string? headerLine = reader.ReadLine();

        // Пустой файл не содержит записей.
        if (headerLine == null)
            return events;

        // Первая строка содержит названия колонок.
        string[] headers = headerLine.TrimStart('\uFEFF').Split(',');

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] values = line.Split(',');

            if (values.Length != headers.Length)
                throw new InvalidDataException("Кількість значень не збігається з кількістю колонок.");

            var record = new Dictionary<string, string>();

            for (int i = 0; i < headers.Length; i++)
                record[headers[i]] = values[i];

            events.Add(record);
        }

        return events;
    }

    public static Dictionary<int, int> ClassCounts(IReadOnlyList<Dictionary<string, string>> events)
    {
        var counts = new Dictionary<int, int>
        {
            [0] = 0,
            [1] = 0,
            [2] = 0
        };

        foreach (var record in events)
        {
            if (!record.TryGetValue("class", out string? value))
                throw new InvalidDataException("CSV не містить колонку 'class'.");

            if (!int.TryParse(value, out int classId) || !counts.ContainsKey(classId))
            {
                throw new InvalidDataException($"Некоректний клас '{value}'. Очікується 0, 1 або 2.");
            }

            counts[classId]++;
        }

        return counts;
    }
}