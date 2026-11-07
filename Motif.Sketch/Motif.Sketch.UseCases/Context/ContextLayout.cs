namespace Motif.Sketch;

public sealed record LayoutOptions
{
    /// <summary>Space between the edge of the system and the nearest column.</summary>
    public double ZoneGap { get; init; } = 140;
    /// <summary>The zone gap grows, up to this, when a relationship label would otherwise be cut short.</summary>
    public double MaxZoneGap { get; init; } = 300;
    public double ColumnGap { get; init; } = 60;
    public double RowGap { get; init; } = 40;
    /// <summary>Extra space between the secondary / external / storage groups.</summary>
    public double GroupGap { get; init; } = 50;
    public int MaxPerColumn { get; init; } = 5;
    /// <summary>Clearance kept around pinned elements and already-placed ones.</summary>
    public double Padding { get; init; } = 20;
}

/// <summary>
/// Context Sketch's layout: the system at the origin, primary actors in one column on the left, and
/// on the right secondary actors, external systems and storage, top to bottom. Pinned elements are
/// never moved. Deterministic, and nothing overlaps.
/// </summary>
public static class ContextLayout
{
    private static readonly ElementType[] RightGroups =
        [ElementType.SecondaryActor, ElementType.ExternalSystem, ElementType.Storage];

    public static Size SizeOf(ElementType type) => type == ElementType.System ? DiagramStyle.SystemSize : DiagramStyle.ElementSize;

    public static Rect BoundsOf(Element element) => Rect.Around(new Point(element.X, element.Y), SizeOf(element.Type));

    /// <summary>Centre positions for every unpinned element.</summary>
    public static Dictionary<string, Point> Compute(ContextDiagram diagram, LayoutOptions? options = null)
    {
        options ??= new LayoutOptions();
        var result = new Dictionary<string, Point>();

        var system = diagram.System;
        var anchor = system.Pinned ? new Point(system.X, system.Y) : new Point(0, 0);
        if (!system.Pinned) result[system.Id] = anchor;

        // Everything that must not be overlapped: the system and all pinned elements.
        var occupied = new List<Rect> { Rect.Around(anchor, DiagramStyle.SystemSize) };
        occupied.AddRange(diagram.Elements.Where(e => e.Pinned && e.Type != ElementType.System).Select(BoundsOf));

        var sysW = DiagramStyle.SystemSize.Width;
        var size = DiagramStyle.ElementSize;
        var pitch = size.Height + options.RowGap;

        // Left zone: one tall column of primary actors, centred on the system.
        var primaries = Unpinned(diagram, ElementType.PrimaryActor);
        var leftX = anchor.X - sysW / 2 - GapFor(diagram, primaries, options) - size.Width / 2;
        for (var i = 0; i < primaries.Count; i++)
        {
            var ideal = new Point(leftX, anchor.Y + (i - (primaries.Count - 1) / 2.0) * pitch);
            Place(primaries[i], ideal, size, pitch, options, occupied, result);
        }

        // Right zone: secondary actors, then external systems, then storage, top to bottom.
        var groups = RightGroups.Select(t => Unpinned(diagram, t)).Where(g => g.Count > 0).ToList();
        var rows = groups.Select(g => Math.Min(g.Count, options.MaxPerColumn)).ToList();
        var totalHeight = rows.Sum(r => r * size.Height + (r - 1) * options.RowGap)
                          + (groups.Count - 1) * (options.RowGap + options.GroupGap);
        var top = anchor.Y - totalHeight / 2;
        var firstColumnX = anchor.X + sysW / 2 + GapFor(diagram, groups.SelectMany(g => g), options) + size.Width / 2;

        for (var g = 0; g < groups.Count; g++)
        {
            for (var i = 0; i < groups[g].Count; i++)
            {
                var column = i / options.MaxPerColumn;
                var row = i % options.MaxPerColumn;
                var ideal = new Point(
                    firstColumnX + column * (size.Width + options.ColumnGap),
                    top + size.Height / 2 + row * pitch);
                Place(groups[g][i], ideal, size, pitch, options, occupied, result);
            }
            top += rows[g] * size.Height + (rows[g] - 1) * options.RowGap + options.RowGap + options.GroupGap;
        }

        return result;
    }

    /// <summary>Moves every unpinned element to its place. Returns true if anything moved.</summary>
    public static bool Apply(ContextDiagram diagram, LayoutOptions? options = null)
    {
        var moved = false;
        foreach (var (id, p) in Compute(diagram, options))
        {
            var element = diagram.Elements.First(e => e.Id == id);
            if (element.X == p.X && element.Y == p.Y) continue;
            element.X = p.X;
            element.Y = p.Y;
            moved = true;
        }
        return moved;
    }

    /// <summary>
    /// The gap between the system and a zone: the usual one, widened (up to a cap) so the longest label
    /// in the zone fits uncut. The horizontal gap is the shortest any of the zone's arrows can be.
    /// </summary>
    private static double GapFor(ContextDiagram diagram, IEnumerable<Element> zone, LayoutOptions options) =>
        zone.Select(e => TextWrap.ArrowLengthFor(diagram.RelationshipOf(e.Id)?.Label, options.ZoneGap, options.MaxZoneGap))
            .DefaultIfEmpty(options.ZoneGap)
            .Max();

    private static List<Element> Unpinned(ContextDiagram diagram, ElementType type) =>
        diagram.Elements.Where(e => e.Type == type && !e.Pinned).ToList();

    /// <summary>Puts the element at its ideal slot, nudging it down past anything already in the way.</summary>
    private static void Place(Element element, Point ideal, Size size, double step, LayoutOptions options,
        List<Rect> occupied, Dictionary<string, Point> result)
    {
        var position = ideal;
        var clearance = options.Padding / 2;
        while (occupied.Any(o => o.Inflate(clearance).Intersects(Rect.Around(position, size).Inflate(clearance))))
            position = position with { Y = position.Y + step };

        occupied.Add(Rect.Around(position, size));
        result[element.Id] = position;
    }
}
