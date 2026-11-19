using System.Globalization;
using System.Text;

namespace LogKeeper.Experiments;

/// <summary>Таблиця результатів: пишеться одночасно в results/&lt;name&gt;.csv і results/&lt;name&gt;.md.</summary>
public sealed class ResultTable(string name, string title, params string[] columns)
{
    private readonly List<string[]> rows = new();
    private readonly List<string> notes = new();

    public string Name { get; } = name;

    public void Add(params object[] values) =>
        rows.Add(values.Select(Format).ToArray());

    public void Note(string text) => notes.Add(text);

    public static string Format(object v) => v switch
    {
        double d => d.ToString(Math.Abs(d) >= 1000 || d == Math.Floor(d) && Math.Abs(d) < 1e9 ? "0.##" : "0.0000", CultureInfo.InvariantCulture),
        float f => Format((double)f),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => v?.ToString() ?? "",
    };

    public void Save(string directory)
    {
        Directory.CreateDirectory(directory);

        var csv = new StringBuilder();
        csv.AppendLine(string.Join(",", columns));
        foreach (var r in rows) csv.AppendLine(string.Join(",", r.Select(Escape)));
        File.WriteAllText(Path.Combine(directory, Name + ".csv"), csv.ToString());

        var md = new StringBuilder();
        md.AppendLine($"# {title}").AppendLine();
        md.AppendLine("| " + string.Join(" | ", columns) + " |");
        md.AppendLine("|" + string.Concat(columns.Select(_ => "---|")));
        foreach (var r in rows) md.AppendLine("| " + string.Join(" | ", r) + " |");
        if (notes.Count > 0)
        {
            md.AppendLine();
            foreach (var n in notes) md.AppendLine("- " + n);
        }
        File.WriteAllText(Path.Combine(directory, Name + ".md"), md.ToString());

        Console.WriteLine(md.ToString());
    }

    private static string Escape(string s) => s.Contains(',') || s.Contains('"') ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
}
