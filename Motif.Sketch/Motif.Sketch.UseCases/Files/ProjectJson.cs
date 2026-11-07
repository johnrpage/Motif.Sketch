using System.Text.Json;
using System.Text.Json.Serialization;

namespace Motif.Sketch;

public sealed record LoadResult(Project? Project, IReadOnlyList<string> Problems, bool NeedsLayout)
{
    public bool Succeeded => Project is not null;
}

/// <summary>
/// The <c>.motifsketch.json</c> format (SPEC §4.1): camelCase, enums as camelCase strings, indented,
/// with property order fixed by the file classes below, so save then load gives an identical file.
/// </summary>
public static class ProjectJson
{
    public const string Extension = ".motifsketch.json";

    private static readonly JsonSerializerOptions Indented = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = ProjectJsonContext.Default,
    };

    private static readonly JsonSerializerOptions Minified = new(Indented) { WriteIndented = false };

    public static string Save(Project project, bool indented = true)
    {
        var d = project.ContextDiagram;
        var file = new ProjectFile
        {
            SchemaVersion = Project.SchemaVersion,
            Title = project.Title,
            ContextDiagram = new ContextDiagramFile
            {
                Elements = d.Elements.Select(e => new ElementFile
                {
                    Id = e.Id,
                    Type = ContextScene.Camel(e.Type),
                    Name = e.Name,
                    Description = Blank(e.Description),
                    Technology = Blank(e.Technology),
                    X = Math.Round(e.X),
                    Y = Math.Round(e.Y),
                    Pinned = e.Pinned,
                }).ToList(),
                Relationships = d.Relationships.Select(r => new RelationshipFile
                {
                    Id = r.Id, From = r.From, To = r.To, Label = r.Label, Direction = ContextScene.Camel(r.Direction),
                }).ToList(),
            },
            Sketch = project.Sketch is not { } s ? null : new SketchFile
            {
                Ports = s.Ports.Select(p => new PortFile
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = Blank(p.Description),
                    Side = ContextScene.Camel(p.Side),
                    IsUser = p.IsUser,
                    Adapters = p.Adapters.Select(a => new AdapterFile
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Technology = Blank(a.Technology),
                        Description = Blank(a.Description),
                        Kind = ContextScene.Camel(a.Kind),
                        Sources = [.. a.Sources],
                        SourceDeleted = a.SourceDeleted ? true : null,
                    }).ToList(),
                }).ToList(),
            },
        };
        var json = JsonSerializer.Serialize(file, indented ? Indented : Minified);
        return indented ? json.ReplaceLineEndings("\n") + "\n" : json;
    }

    /// <summary>Reads a project and checks every rule. Never throws for a bad file: it lists what's wrong.</summary>
    public static LoadResult Load(string json)
    {
        ProjectFile? file;
        try
        {
            file = JsonSerializer.Deserialize<ProjectFile>(json, Indented);
        }
        catch (JsonException ex)
        {
            var where = ex.LineNumber is { } line ? $" (line {line + 1})" : "";
            return Fail($"This isn't a Motif Sketch project file{where}: the JSON can't be read.");
        }
        if (file is null) return Fail("The file is empty.");
        if (file.SchemaVersion != Project.SchemaVersion)
            return Fail(file.SchemaVersion == 0
                ? "This isn't a Motif Sketch project file: it has no schema version."
                : $"This file uses schema version {file.SchemaVersion}, but this app reads version {Project.SchemaVersion}.");
        if (file.ContextDiagram is null) return Fail("The file has no context diagram.");

        var problems = new List<string>();
        var project = new Project { Title = string.IsNullOrWhiteSpace(file.Title) ? "Untitled" : file.Title.Trim() };
        var needsLayout = false;

        foreach (var e in file.ContextDiagram.Elements ?? [])
        {
            if (!TryParse<ElementType>(e.Type, out var type))
            {
                problems.Add($"Element '{e.Id}' has an unknown type '{e.Type}'.");
                continue;
            }
            if (e.X is null || e.Y is null) needsLayout = true;
            project.ContextDiagram.Elements.Add(new Element
            {
                Id = e.Id ?? "",
                Type = type,
                Name = e.Name?.Trim() ?? "",
                Description = Blank(e.Description),
                Technology = Blank(e.Technology),
                X = e.X ?? 0,
                Y = e.Y ?? 0,
                Pinned = e.Pinned == true && e.X is not null && e.Y is not null,
            });
        }

        foreach (var r in file.ContextDiagram.Relationships ?? [])
        {
            var direction = RelationshipDirection.OneWay;
            if (r.Direction is not null && !TryParse(r.Direction, out direction))
            {
                problems.Add($"Relationship '{r.Id}' has an unknown direction '{r.Direction}'.");
                continue;
            }
            project.ContextDiagram.Relationships.Add(new Relationship
            {
                Id = r.Id ?? "", From = r.From ?? "", To = r.To ?? "", Label = r.Label ?? "", Direction = direction,
            });
        }

        if (file.Sketch is { } sketch)
        {
            project.Sketch = new Sketch();
            foreach (var p in sketch.Ports ?? [])
            {
                if (!TryParse<Side>(p.Side, out var side))
                {
                    problems.Add($"Port '{p.Name ?? p.Id}' has an unknown side '{p.Side}'.");
                    continue;
                }
                var port = new Port { Id = p.Id ?? "", Name = p.Name?.Trim() ?? "", Description = Blank(p.Description), Side = side, IsUser = p.IsUser == true };
                foreach (var a in p.Adapters ?? [])
                {
                    if (!TryParse<AdapterKind>(a.Kind, out var kind))
                    {
                        problems.Add($"Adapter '{a.Name ?? a.Id}' has an unknown kind '{a.Kind}'.");
                        continue;
                    }
                    port.Adapters.Add(new Adapter
                    {
                        Id = a.Id ?? "",
                        Name = a.Name?.Trim() ?? "",
                        Technology = Blank(a.Technology),
                        Description = Blank(a.Description),
                        Kind = kind,
                        Sources = [.. a.Sources ?? []],
                        SourceDeleted = a.SourceDeleted == true,
                    });
                }
                project.Sketch.Ports.Add(port);
            }
        }

        // Check the structure only once every item was understood, so problems aren't doubled up.
        if (problems.Count == 0) problems.AddRange(ProjectValidator.Validate(project));
        return problems.Count > 0 ? new LoadResult(null, problems, false) : new LoadResult(project, [], needsLayout);
    }

    private static LoadResult Fail(string problem) => new(null, [problem], false);

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool TryParse<T>(string? text, out T value) where T : struct, Enum =>
        Enum.TryParse(text, ignoreCase: true, out value) && Enum.IsDefined(value) && !int.TryParse(text, out _);

}

// Property order here is the order written to disk.

internal sealed class ProjectFile
{
    public int SchemaVersion { get; set; }
    public string? Title { get; set; }
    public ContextDiagramFile? ContextDiagram { get; set; }
    public SketchFile? Sketch { get; set; }
}

internal sealed class ContextDiagramFile
{
    public List<ElementFile>? Elements { get; set; }
    public List<RelationshipFile>? Relationships { get; set; }
}

internal sealed class ElementFile
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Technology { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public bool? Pinned { get; set; }
}

internal sealed class RelationshipFile
{
    public string? Id { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Label { get; set; }
    public string? Direction { get; set; }
}

internal sealed class SketchFile
{
    public List<PortFile>? Ports { get; set; }
}

internal sealed class PortFile
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Side { get; set; }
    public bool? IsUser { get; set; }
    public List<AdapterFile>? Adapters { get; set; }
}

internal sealed class AdapterFile
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Technology { get; set; }
    public string? Description { get; set; }
    public string? Kind { get; set; }
    public List<string>? Sources { get; set; }
    public bool? SourceDeleted { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ProjectFile))]
internal sealed partial class ProjectJsonContext : JsonSerializerContext;
