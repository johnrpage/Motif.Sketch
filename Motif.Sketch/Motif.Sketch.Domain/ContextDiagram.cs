namespace Motif.Sketch;

public enum ElementType { System, PrimaryActor, SecondaryActor, ExternalSystem, Storage }

/// <summary>Arrows point from whoever starts the interaction; two-way is for when both sides do.</summary>
public enum RelationshipDirection { OneWay, TwoWay }

/// <summary>Something in the context diagram: the system, or a person or system around it.</summary>
public sealed class Element
{
    public required string Id { get; init; }
    public required ElementType Type { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Technology { get; set; }

    /// <summary>Centre of the element, in diagram units.</summary>
    public double X { get; set; }
    public double Y { get; set; }

    /// <summary>True once the user has dragged the element; auto-layout leaves it alone.</summary>
    public bool Pinned { get; set; }

    public Element Clone() => (Element)MemberwiseClone();
}

/// <summary>The one line between an element and the system.</summary>
public sealed class Relationship
{
    public required string Id { get; init; }
    public required string From { get; set; }
    public required string To { get; set; }
    public string Label { get; set; } = "";
    public RelationshipDirection Direction { get; set; } = RelationshipDirection.OneWay;

    public Relationship Clone() => (Relationship)MemberwiseClone();
}

/// <summary>A system context diagram: one system, and everything around it linked to it once.</summary>
public sealed class ContextDiagram
{
    public List<Element> Elements { get; } = [];
    public List<Relationship> Relationships { get; } = [];

    public Element System => Elements.Single(e => e.Type == ElementType.System);

    public Element? Find(string elementId) => Elements.Find(e => e.Id == elementId);

    public Relationship? RelationshipOf(string elementId) =>
        Relationships.Find(r => r.From == elementId || r.To == elementId);

    public ContextDiagram Clone()
    {
        var copy = new ContextDiagram();
        copy.Elements.AddRange(Elements.Select(e => e.Clone()));
        copy.Relationships.AddRange(Relationships.Select(r => r.Clone()));
        return copy;
    }
}
