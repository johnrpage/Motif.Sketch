using System.Text;

namespace Motif.Sketch;

/// <summary>Turns display names into C# names: "In-memory store" becomes "InMemoryStore".</summary>
public static class CodeName
{
    /// <summary>
    /// PascalCase words. Longer all-capital words become Pascal ("SVG" is "Svg"), but two-letter
    /// acronyms stay in capitals ("UI"), and mixed-case words are kept as typed ("IndexedDB").
    /// </summary>
    public static string From(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";

        var words = new List<string>();
        var word = new StringBuilder();
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c)) word.Append(c);
            else Flush();
        }
        Flush();
        return string.Concat(words.Select(Pascal));

        void Flush()
        {
            if (word.Length > 0) words.Add(word.ToString());
            word.Clear();
        }
    }

    /// <summary>
    /// A solution root: a dotted name keeps its dots ("Motif.Sketch"), each part made a code name.
    /// Without dots it's a plain code name ("Motif Sketch" is "MotifSketch").
    /// </summary>
    public static string Root(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var parts = name.Split('.').Select(From).ToList();
        return parts.Any(p => !IsValid(p)) ? "" : string.Join(".", parts);
    }

    public static bool IsValid(string codeName) => codeName.Length > 0 && !char.IsDigit(codeName[0]);

    private static string Pascal(string word) =>
        word.Length > 2 && word.All(char.IsUpper)
            ? word[0] + word[1..].ToLowerInvariant()
            : char.ToUpperInvariant(word[0]) + word[1..];
}
