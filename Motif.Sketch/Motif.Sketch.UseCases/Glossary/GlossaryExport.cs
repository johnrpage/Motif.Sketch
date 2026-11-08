using System.Text;
using System.Text.RegularExpressions;

namespace Motif.Sketch;

/// <summary>A starting glossary entry for something in the sketch.</summary>
/// <param name="Kinds">Glossary kinds: actor, external system, storage, port. Two when an element and a port share a name.</param>
/// <param name="SketchIds">The ids it comes from, each with "element" or "port" when there's more than one.</param>
public sealed record GlossaryEntry(string Name, string? Definition, IReadOnlyList<string> Kinds, IReadOnlyList<(string Id, string What)> SketchIds);

/// <summary>What Export glossary would write.</summary>
/// <param name="Markdown">The whole glossary: the existing file with the new entries put in, or a new file.</param>
/// <param name="Added">The new entries' names, alphabetically.</param>
/// <param name="AlreadyThere">How many of the sketch's entries the glossary already had.</param>
public sealed record GlossaryMerge(string Markdown, IReadOnlyList<string> Added, int AlreadyThere);

/// <summary>
/// Export glossary: starting entries for <c>motif/glossary.md</c> from the sketch's actors, external systems,
/// storage and ports, or the new ones merged into an existing glossary.
/// </summary>
/// <remarks>
/// The merge never changes a line people wrote. An entry is already there when a term's <b>Sketch</b> field
/// names one of its ids, or a term has its name; everything else is put in as a <i>proposed</i> term, before
/// the first heading that sorts after it, so an alphabetical glossary stays alphabetical. Renames are left to
/// people: a renamed element whose id is still in the glossary counts as already there.
/// </remarks>
public static partial class GlossaryExport
{
    public const string FileName = "glossary.md";

    public static GlossaryMerge Merge(Project project, string? existing)
    {
        var entries = EntriesOf(project);
        if (existing is null)
        {
            return new GlossaryMerge(NewGlossary(entries), entries.Select(e => e.Name).ToList(), 0);
        }

        var newline = existing.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = existing.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        if (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
        var terms = TermsOf(lines);
        var knownIds = terms.SelectMany(t => t.SketchIds).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var knownNames = terms.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = entries
            .Where(e => !knownNames.Contains(e.Name) && !e.SketchIds.Any(s => knownIds.Contains(s.Id)))
            .ToList();
        if (added.Count == 0) return new GlossaryMerge(existing, [], entries.Count);

        // Insert from the bottom up, so the line numbers found for the earlier ones stay right.
        var inserts = added
            .Select(e => (Entry: e, At: terms.FirstOrDefault(t => Compare(t.Name, e.Name) > 0)?.Line))
            .ToList();
        var atEnd = inserts.Where(i => i.At is null).Select(i => i.Entry).ToList();
        if (atEnd.Count > 0)
        {
            if (lines.Count > 0 && lines[^1].Trim().Length > 0) lines.Add("");
            lines.AddRange(atEnd.SelectMany(e => Lines(e).Append("")));
            lines.RemoveAt(lines.Count - 1);
        }
        foreach (var group in inserts.Where(i => i.At is not null).GroupBy(i => i.At!.Value).OrderByDescending(g => g.Key))
        {
            lines.InsertRange(group.Key, group.SelectMany(i => Lines(i.Entry).Append("")));
        }

        var markdown = string.Join(newline, lines) + newline;
        return new GlossaryMerge(markdown, added.Select(e => e.Name).ToList(), entries.Count - added.Count);
    }

    /// <summary>The sketch's starting entries, alphabetically: one per name, so an element and its port share one.</summary>
    public static IReadOnlyList<GlossaryEntry> EntriesOf(Project project)
    {
        var items = new List<(string Name, string? Description, string Kind, string Id, string What)>();
        foreach (var e in project.ContextDiagram.Elements)
        {
            var kind = e.Type switch
            {
                ElementType.PrimaryActor or ElementType.SecondaryActor => "actor",
                ElementType.ExternalSystem => "external system",
                ElementType.Storage => "storage",
                _ => null,
            };
            if (kind is not null && e.Name.Trim().Length > 0) items.Add((e.Name.Trim(), e.Description, kind, e.Id, "element"));
        }
        foreach (var p in project.Sketch?.Ports ?? [])
        {
            if (p.Name.Trim().Length > 0) items.Add((p.Name.Trim(), p.Description, "port", p.Id, "port"));
        }

        return items
            .GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new GlossaryEntry(
                g.First().Name,
                Sentence(g.Select(i => i.Description).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d))),
                g.Select(i => i.Kind).Distinct().ToList(),
                g.Select(i => (i.Id, i.What)).ToList()))
            .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NewGlossary(IReadOnlyList<GlossaryEntry> entries)
    {
        var lines = new List<string>
        {
            "# Glossary",
            "",
            "The words this project uses, and what they mean. People and agents use these terms in code, docs, issues and discussion, and avoid the words listed under **Avoid**. A new concept gets an entry, marked *proposed*, in the same change that introduces it. An entry with no status is agreed.",
            "",
            "The first entries came from the sketch ([sketch.motifsketch.json](sketch.motifsketch.json)): **Sketch** gives the id of the element or port each one comes from. The format is described in the [Motif Glossary design note](https://github.com/motif-dotnet/.github/blob/main/design/glossary.md).",
        };
        foreach (var e in entries)
        {
            lines.Add("");
            lines.AddRange(Lines(e));
        }
        return string.Join("\n", lines) + "\n";
    }

    private static IEnumerable<string> Lines(GlossaryEntry e)
    {
        yield return $"## {e.Name}";
        yield return "";
        if (e.Definition is { } definition)
        {
            yield return definition;
            yield return "";
        }
        yield return $"- **Kind:** {string.Join(", ", e.Kinds)}";
        yield return "- **Status:** proposed";
        yield return "- **Sketch:** " + (e.SketchIds.Count == 1
            ? $"`{e.SketchIds[0].Id}`"
            : string.Join(", ", e.SketchIds.Select(s => $"`{s.Id}` ({s.What})")));
    }

    private static string? Sentence(string? text)
    {
        var t = string.Join(' ', (text ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (t.Length == 0) return null;
        t = char.ToUpperInvariant(t[0]) + t[1..];
        return ".!?".Contains(t[^1]) ? t : t + ".";
    }

    private static int Compare(string a, string b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase);

    private sealed record ExistingTerm(string Name, int Line, List<string> SketchIds);

    /// <summary>The <c>##</c> terms in a glossary, with the line each starts on and the ids in its Sketch field.</summary>
    private static List<ExistingTerm> TermsOf(List<string> lines)
    {
        var terms = new List<ExistingTerm>();
        string? fence = null;
        for (var n = 0; n < lines.Count; n++)
        {
            var trimmed = lines[n].Trim();
            if (fence is not null)
            {
                if (trimmed.StartsWith(fence, StringComparison.Ordinal)) fence = null;
                continue;
            }
            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                fence = trimmed[..3];
                continue;
            }
            if (TermHeading().Match(lines[n]) is { Success: true } heading)
            {
                var name = heading.Groups["text"].Value.Trim().TrimEnd('#').Trim().Replace("`", "").Replace("*", "").Replace("_", " ").Trim();
                terms.Add(new ExistingTerm(name, n, []));
                continue;
            }
            if (terms.Count > 0 && SketchField().Match(lines[n]) is { Success: true } field)
            {
                var value = field.Groups["value"].Value;
                var codes = CodeSpan().Matches(value).Select(m => m.Groups[1].Value.Trim()).ToList();
                terms[^1].SketchIds.AddRange(codes.Count > 0
                    ? codes
                    : value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "")
                        .Where(id => id.Length > 0));
            }
        }
        return terms;
    }

    [GeneratedRegex(@"^ {0,3}##\s+(?<text>.+)$")]
    private static partial Regex TermHeading();

    [GeneratedRegex(@"^\s{0,3}[-*+]\s+\*\*\s*sketch\s*:?\s*\*\*\s*:?\s*(?<value>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex SketchField();

    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex CodeSpan();
}
