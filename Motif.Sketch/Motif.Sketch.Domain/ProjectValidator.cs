namespace Motif.Sketch;

/// <summary>The load rules of SPEC §4.4. An empty list means the project is usable.</summary>
public static class ProjectValidator
{
    public static List<string> Validate(Project project)
    {
        var problems = new List<string>();
        var diagram = project.ContextDiagram;

        var systems = diagram.Elements.Count(e => e.Type == ElementType.System);
        if (systems != 1)
            problems.Add($"There must be exactly one system, but there are {systems}.");

        var sketch = project.Sketch;
        var ids = diagram.Elements.Select(e => e.Id)
            .Concat(diagram.Relationships.Select(r => r.Id))
            .Concat(sketch?.Ports.Select(p => p.Id) ?? [])
            .Concat(sketch?.Adapters.Select(a => a.Id) ?? [])
            .ToList();
        if (ids.Any(string.IsNullOrWhiteSpace))
            problems.Add("Something has no id.");
        foreach (var duplicate in ids.Where(id => !string.IsNullOrWhiteSpace(id)).GroupBy(id => id).Where(g => g.Count() > 1))
            problems.Add($"The id '{duplicate.Key}' is used more than once.");

        foreach (var e in diagram.Elements.Where(e => string.IsNullOrWhiteSpace(e.Name)))
            problems.Add($"Element '{e.Id}' has no name.");

        var byId = diagram.Elements.GroupBy(e => e.Id).ToDictionary(g => g.Key, g => g.First());
        var systemId = systems == 1 ? diagram.System.Id : null;
        foreach (var r in diagram.Relationships)
        {
            var missing = new[] { r.From, r.To }.Where(end => !byId.ContainsKey(end)).ToList();
            if (missing.Count > 0)
                problems.Add($"Relationship '{r.Id}' refers to '{string.Join("' and '", missing)}', which isn't an element.");
            else if (r.From == r.To)
                problems.Add($"Relationship '{r.Id}' links an element to itself.");
            else if (systemId is not null && r.From != systemId && r.To != systemId)
                problems.Add($"Relationship '{r.Id}' doesn't have the system at one end.");
        }

        foreach (var e in diagram.Elements.Where(e => e.Type != ElementType.System))
        {
            var count = diagram.Relationships.Count(r => r.From == e.Id || r.To == e.Id);
            if (count != 1)
                problems.Add($"'{e.Name}' must have exactly one relationship, with the system, but has {count}.");
        }

        if (sketch is null) return problems;

        var users = sketch.Ports.Count(p => p.IsUser);
        if (users != 1)
            problems.Add(users == 0 ? "The sketch has no User port." : $"The sketch has {users} User ports; it needs exactly one.");
        if (sketch.Ports.Any(p => p.IsUser && p.Side != Side.Driving))
            problems.Add("The User port must be on the driving side.");

        foreach (var port in sketch.Ports.Where(p => p.Side == Side.Driven))
            foreach (var adapter in port.Adapters.Where(a => a.Kind == AdapterKind.Checks))
                problems.Add($"'{adapter.Name}' on '{port.Name}' is a checks adapter, but checks belong on driving ports only.");

        var sources = sketch.Adapters.SelectMany(a => a.Sources).ToList();
        foreach (var source in sources.Distinct())
        {
            if (!byId.TryGetValue(source, out var element) || element.Type == ElementType.System)
                problems.Add($"An adapter stands for '{source}', which isn't an element of the context diagram.");
            else if (sources.Count(s => s == source) > 1)
                problems.Add($"'{element.Name}' is a source of more than one adapter.");
        }

        return problems;
    }
}
