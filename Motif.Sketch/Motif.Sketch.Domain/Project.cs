namespace Motif.Sketch;

/// <summary>
/// One system: its context diagram and, once started, its ports-and-adapters sketch. The sketch
/// refers to context diagram elements by id, so nothing is copied and nothing can drift. Every rule
/// lives here, including how the sketch follows changes to the context diagram (SPEC §5.3).
/// </summary>
public sealed class Project
{
    public const int SchemaVersion = 1;
    public const string UserPortName = "User";
    public const string SpecsName = "Specs";
    public const string SpecsTechnology = "xUnit";

    public string Title { get; set; } = "Untitled";
    public ContextDiagram ContextDiagram { get; init; } = new();
    public Sketch? Sketch { get; set; }

    public static Project CreateNew(string title = "Untitled", string systemName = "My System")
    {
        var project = new Project { Title = title };
        project.ContextDiagram.Elements.Add(new Element { Id = project.NewId(), Type = ElementType.System, Name = systemName });
        return project;
    }

    public Project Clone() => new() { Title = Title, ContextDiagram = ContextDiagram.Clone(), Sketch = Sketch?.Clone() };

    public void Rename(string title) => Title = Required(title, "The project needs a title.");

    // ---- Context diagram -------------------------------------------------------------------

    /// <summary>
    /// Adds an element with its one relationship to the system. Primary actors point at the system;
    /// the system points at everything else. A new element waits in the sketch's unplaced list.
    /// </summary>
    public Element AddElement(ElementType type)
    {
        if (type == ElementType.System)
            throw new ProjectRuleException("A project has exactly one system.");

        var stem = DefaultName(type);
        var n = ContextDiagram.Elements.Count(e => e.Type == type) + 1;
        while (ContextDiagram.Elements.Any(e => e.Name.Equals($"{stem} {n}", StringComparison.OrdinalIgnoreCase))) n++;

        var element = new Element { Id = NewId(), Type = type, Name = $"{stem} {n}" };
        var system = ContextDiagram.System;
        var incoming = type == ElementType.PrimaryActor;
        ContextDiagram.Elements.Add(element);
        ContextDiagram.Relationships.Add(new Relationship
        {
            Id = NewId(),
            From = incoming ? element.Id : system.Id,
            To = incoming ? system.Id : element.Id,
        });
        return element;
    }

    /// <summary>Renames an element. An adapter still carrying the element's old name follows the rename.</summary>
    public void RenameElement(string elementId, string name)
    {
        var element = FindElement(elementId);
        var newName = Required(name, "An element needs a name.");
        foreach (var adapter in Sketch?.Adapters ?? [])
            if (adapter.Sources.Contains(elementId) && adapter.Name == element.Name)
                adapter.Name = newName;
        element.Name = newName;
    }

    public void DescribeElement(string elementId, string? description) => FindElement(elementId).Description = Tidy(description);

    public void SetElementTechnology(string elementId, string? technology) => FindElement(elementId).Technology = Tidy(technology);

    /// <summary>Changes what kind of element this is. Its placement in the sketch is kept.</summary>
    public void SetElementType(string elementId, ElementType type)
    {
        var element = FindElement(elementId);
        if (element.Type == ElementType.System || type == ElementType.System)
            throw new ProjectRuleException("There is exactly one system; its type can't change and nothing else can become it.");
        element.Type = type;
    }

    /// <summary>
    /// Deletes an element and its relationship. Any adapter it was a source of keeps the rest of its
    /// sources and is flagged "source deleted". The system can't be deleted.
    /// </summary>
    public void DeleteElement(string elementId)
    {
        var element = FindElement(elementId);
        if (element.Type == ElementType.System)
            throw new ProjectRuleException("The system can't be deleted.");
        ContextDiagram.Relationships.RemoveAll(r => r.From == elementId || r.To == elementId);
        ContextDiagram.Elements.Remove(element);
        foreach (var adapter in Sketch?.Adapters ?? [])
            if (adapter.Sources.Remove(elementId))
                adapter.SourceDeleted = true;
    }

    /// <summary>Moves an element and pins it, so auto-layout leaves it where the user put it.</summary>
    public void MoveElement(string elementId, double x, double y)
    {
        var element = FindElement(elementId);
        element.X = x;
        element.Y = y;
        element.Pinned = true;
    }

    public void SetRelationshipLabel(string relationshipId, string? label) => FindRelationship(relationshipId).Label = label?.Trim() ?? "";

    public void SetRelationshipDirection(string relationshipId, RelationshipDirection direction) =>
        FindRelationship(relationshipId).Direction = direction;

    public void ReleasePins()
    {
        foreach (var e in ContextDiagram.Elements) e.Pinned = false;
    }

    public Element FindElement(string elementId) =>
        ContextDiagram.Find(elementId) ?? throw new ProjectRuleException($"There is no element '{elementId}'.");

    public Relationship FindRelationship(string relationshipId) =>
        ContextDiagram.Relationships.Find(r => r.Id == relationshipId)
        ?? throw new ProjectRuleException($"There is no relationship '{relationshipId}'.");

    // ---- Sketch ----------------------------------------------------------------------------

    /// <summary>
    /// Starts the sketch: the User port with its Specs, and one Real adapter for each primary actor.
    /// Everything else waits in the unplaced list.
    /// </summary>
    public Sketch StartSketch()
    {
        if (Sketch is not null)
            throw new ProjectRuleException("The sketch has already been started.");
        var user = new Port { Id = NewId(), Name = UserPortName, Side = Side.Driving, IsUser = true };
        Sketch = new Sketch { Ports = [user] };
        foreach (var actor in ContextDiagram.Elements.Where(e => e.Type == ElementType.PrimaryActor))
            user.Adapters.Add(AdapterFrom(actor));
        user.Adapters.Add(SpecsAdapter());
        return Sketch;
    }

    /// <summary>Context diagram elements that no adapter stands for yet, in diagram order.</summary>
    public IReadOnlyList<Element> Unplaced
    {
        get
        {
            if (Sketch is null) return [];
            var placed = Sketch.Adapters.SelectMany(a => a.Sources).ToHashSet();
            return ContextDiagram.Elements.Where(e => e.Type != ElementType.System && !placed.Contains(e.Id)).ToList();
        }
    }

    /// <summary>Adds a port with a default name. A driving port starts with its Specs.</summary>
    public Port AddPort(Side side)
    {
        var sketch = RequireSketch();
        var port = NewPort(NextName("Port", sketch.Ports.Select(p => p.Name)), side);
        sketch.Ports.Add(port);
        return port;
    }

    public void RenamePort(string portId, string name) => FindPort(portId).Name = Required(name, "A port needs a name.");

    public void DescribePort(string portId, string? description) => FindPort(portId).Description = Tidy(description);

    /// <summary>Deletes a port and its adapters; the elements they stood for return to the unplaced list.</summary>
    public void DeletePort(string portId)
    {
        var port = FindPort(portId);
        if (port.IsUser)
            throw new ProjectRuleException("The User port can't be deleted.");
        RequireSketch().Ports.Remove(port);
    }

    /// <summary>Moves a port to a new position among the ports on its own side.</summary>
    public void MovePort(string portId, int indexOnSide)
    {
        var sketch = RequireSketch();
        var port = FindPort(portId);
        var sameSide = sketch.PortsOn(port.Side).Where(p => p != port).ToList();
        sameSide.Insert(Math.Clamp(indexOnSide, 0, sameSide.Count), port);
        var otherSide = sketch.Ports.Where(p => p.Side != port.Side).ToList();
        sketch.Ports.Clear();
        sketch.Ports.AddRange(port.Side == Side.Driving ? sameSide.Concat(otherSide) : otherSide.Concat(sameSide));
    }

    /// <summary>Makes a new adapter on a port standing for an unplaced element.</summary>
    public Adapter Place(string elementId, string portId, int? index = null)
    {
        var element = Placeable(elementId);
        var port = FindPort(portId);
        var adapter = AdapterFrom(element);
        Insert(port, adapter, index);
        return adapter;
    }

    /// <summary>Makes a new port named after an unplaced element, with the element as its adapter.</summary>
    public Adapter PlaceOnNewPort(string elementId, Side side)
    {
        var element = Placeable(elementId);
        var sketch = RequireSketch();
        var port = NewPort(UniqueName(element.Name, sketch.Ports.Select(p => p.Name)), side);
        sketch.Ports.Add(port);
        var adapter = AdapterFrom(element);
        port.Adapters.Insert(0, adapter);
        return adapter;
    }

    /// <summary>Adds an unplaced element to an existing adapter's sources.</summary>
    public void MergeInto(string elementId, string adapterId)
    {
        var element = Placeable(elementId);
        var adapter = FindAdapter(adapterId);
        adapter.Sources.Add(element.Id);
        adapter.SourceDeleted = false;
    }

    /// <summary>Takes one source off an adapter; the element returns to the unplaced list.</summary>
    public void RemoveSource(string adapterId, string elementId)
    {
        var adapter = FindAdapter(adapterId);
        if (!adapter.Sources.Remove(elementId))
            throw new ProjectRuleException($"'{adapter.Name}' doesn't stand for that element.");
        adapter.SourceDeleted = false;
    }

    /// <summary>
    /// Adds a hand-made adapter. Fakes are named "Fake ⟨Port⟩", simulators "Simulated ⟨Port⟩" and
    /// checks "Specs" (xUnit). Checks belong on driving ports only.
    /// </summary>
    public Adapter AddAdapter(string portId, AdapterKind kind = AdapterKind.Real)
    {
        var port = FindPort(portId);
        CheckKind(port, kind);
        var taken = port.Adapters.Select(a => a.Name);
        var adapter = kind switch
        {
            AdapterKind.Checks => SpecsAdapter(),
            AdapterKind.Real => new Adapter { Id = NewId(), Name = NextName("Adapter", RequireSketch().Adapters.Select(a => a.Name)) },
            _ => new Adapter { Id = NewId(), Name = UniqueName($"{kind} {port.Name}", taken), Kind = kind },
        };
        if (kind == AdapterKind.Checks) adapter.Name = UniqueName(SpecsName, taken);
        port.Adapters.Add(adapter);
        return adapter;
    }

    /// <summary>Moves an adapter to a port (its own or another) at the given position.</summary>
    public void MoveAdapter(string adapterId, string portId, int? index = null)
    {
        var adapter = FindAdapter(adapterId);
        var target = FindPort(portId);
        CheckKind(target, adapter.Kind);
        var from = PortOf(adapterId);
        var oldIndex = from.Adapters.IndexOf(adapter);
        from.Adapters.Remove(adapter);
        if (from == target && index > oldIndex) index--;
        Insert(target, adapter, index);
    }

    /// <summary>Removes an adapter; the elements it stood for return to the unplaced list.</summary>
    public void RemoveAdapter(string adapterId) => PortOf(adapterId).Adapters.Remove(FindAdapter(adapterId));

    public void RenameAdapter(string adapterId, string name) =>
        Touch(adapterId).Name = Required(name, "An adapter needs a name.");

    public void SetAdapterTechnology(string adapterId, string? technology) => Touch(adapterId).Technology = Tidy(technology);

    public void DescribeAdapter(string adapterId, string? description) => Touch(adapterId).Description = Tidy(description);

    public void SetAdapterKind(string adapterId, AdapterKind kind)
    {
        CheckKind(PortOf(adapterId), kind);
        Touch(adapterId).Kind = kind;
    }

    public Port FindPort(string portId) =>
        RequireSketch().Ports.Find(p => p.Id == portId) ?? throw new ProjectRuleException($"There is no port '{portId}'.");

    public Adapter FindAdapter(string adapterId) =>
        RequireSketch().Adapters.FirstOrDefault(a => a.Id == adapterId)
        ?? throw new ProjectRuleException($"There is no adapter '{adapterId}'.");

    public Port PortOf(string adapterId) =>
        RequireSketch().Ports.Find(p => p.Adapters.Any(a => a.Id == adapterId))
        ?? throw new ProjectRuleException($"There is no adapter '{adapterId}'.");

    // ---- Helpers ---------------------------------------------------------------------------

    public static string DefaultName(ElementType type) => type switch
    {
        ElementType.PrimaryActor => "Primary Actor",
        ElementType.SecondaryActor => "Secondary Actor",
        ElementType.ExternalSystem => "External System",
        ElementType.Storage => "Storage",
        _ => "System",
    };

    private Sketch RequireSketch() =>
        Sketch ?? throw new ProjectRuleException("Start the sketch first.");

    /// <summary>An edit to an adapter clears its "source deleted" flag.</summary>
    private Adapter Touch(string adapterId)
    {
        var adapter = FindAdapter(adapterId);
        adapter.SourceDeleted = false;
        return adapter;
    }

    private static void CheckKind(Port port, AdapterKind kind)
    {
        if (kind == AdapterKind.Checks && port.Side == Side.Driven)
            throw new ProjectRuleException("Checks drive the application, so they belong on driving ports only.");
    }

    private Port NewPort(string name, Side side)
    {
        var port = new Port { Id = NewId(), Name = name, Side = side };
        if (side == Side.Driving) port.Adapters.Add(SpecsAdapter());
        return port;
    }

    private Adapter SpecsAdapter() =>
        new() { Id = NewId(), Name = SpecsName, Technology = SpecsTechnology, Kind = AdapterKind.Checks };

    // A placed element's technology becomes the adapter's. For a primary actor it says how they reach the system.
    private Adapter AdapterFrom(Element element) => new()
    {
        Id = NewId(),
        Name = element.Name,
        Technology = Tidy(element.Technology),
        Description = Tidy(element.Description),
        Kind = AdapterKind.Real,
        Sources = [element.Id],
    };

    private static void Insert(Port port, Adapter adapter, int? index) =>
        port.Adapters.Insert(Math.Clamp(index ?? port.Adapters.Count, 0, port.Adapters.Count), adapter);

    /// <summary>The element, if it can be placed: it exists, isn't the system, and no adapter stands for it yet.</summary>
    private Element Placeable(string elementId)
    {
        var element = FindElement(elementId);
        if (element.Type == ElementType.System)
            throw new ProjectRuleException("The system is the hexagon; it can't become an adapter.");
        if (RequireSketch().Adapters.Any(a => a.Sources.Contains(elementId)))
            throw new ProjectRuleException($"'{element.Name}' is already placed.");
        return element;
    }

    public string NewId()
    {
        var used = ContextDiagram.Elements.Select(e => e.Id)
            .Concat(ContextDiagram.Relationships.Select(r => r.Id))
            .Concat(Sketch?.Ports.Select(p => p.Id) ?? [])
            .Concat(Sketch?.Adapters.Select(a => a.Id) ?? [])
            .ToHashSet();
        string id;
        do id = Random.Shared.Next().ToString("x8"); while (used.Contains(id));
        return id;
    }

    private static string UniqueName(string name, IEnumerable<string> taken)
    {
        var names = taken.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!names.Contains(name)) return name;
        var n = 2;
        while (names.Contains($"{name} {n}")) n++;
        return $"{name} {n}";
    }

    private static string NextName(string stem, IEnumerable<string> taken)
    {
        var names = taken.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var n = 1;
        while (names.Contains($"{stem} {n}")) n++;
        return $"{stem} {n}";
    }

    private static string Required(string? text, string problem) =>
        string.IsNullOrWhiteSpace(text) ? throw new ProjectRuleException(problem) : text.Trim();

    private static string? Tidy(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
