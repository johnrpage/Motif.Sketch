namespace Motif.Sketch;

/// <summary>Which side of the hexagon a port is on. Left is driving, right is driven.</summary>
public enum Side { Driving, Driven }

/// <summary>
/// Real adapters are the production ones; Fake and Simulated stand in for them. Checks drive the
/// application and check what it does, so they belong on driving ports only.
/// </summary>
public enum AdapterKind { Real, Fake, Simulated, Checks }

/// <summary>One concrete way of plugging something into a port.</summary>
public sealed class Adapter
{
    public required string Id { get; init; }
    public string Name { get; set; } = "";
    public string? Technology { get; set; }
    public string? Description { get; set; }
    public AdapterKind Kind { get; set; }

    /// <summary>The context diagram elements this adapter stands for. Empty for a hand-made one.</summary>
    public List<string> Sources { get; init; } = [];

    /// <summary>A source element was deleted; cleared once the user edits, re-sources or deletes the adapter.</summary>
    public bool SourceDeleted { get; set; }

    public Adapter Clone() => new()
    {
        Id = Id, Name = Name, Technology = Technology, Description = Description, Kind = Kind,
        Sources = [.. Sources], SourceDeleted = SourceDeleted,
    };
}

/// <summary>A named role the application plays or needs. Adapters plug into it.</summary>
public sealed class Port
{
    public required string Id { get; init; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public required Side Side { get; init; }

    /// <summary>The User port: always present, always driving, never deleted.</summary>
    public bool IsUser { get; init; }

    public List<Adapter> Adapters { get; init; } = [];

    public Port Clone() => new()
    {
        Id = Id, Name = Name, Description = Description, Side = Side, IsUser = IsUser,
        Adapters = [.. Adapters.Select(a => a.Clone())],
    };
}

/// <summary>The ports-and-adapters sketch: the ports around the hexagon and the adapters on them.</summary>
public sealed class Sketch
{
    public List<Port> Ports { get; init; } = [];

    public Port UserPort => Ports.Single(p => p.IsUser);

    public IEnumerable<Port> PortsOn(Side side) => Ports.Where(p => p.Side == side);

    public IEnumerable<Adapter> Adapters => Ports.SelectMany(p => p.Adapters);

    public Sketch Clone() => new() { Ports = [.. Ports.Select(p => p.Clone())] };
}
