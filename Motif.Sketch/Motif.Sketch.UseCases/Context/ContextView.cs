namespace Motif.Sketch;

public sealed record ElementView(
    string Id,
    ElementType Type,
    string Name,
    string? Description,
    string? Technology,
    bool Pinned,
    Rect Box,
    IReadOnlyList<string> NameLines,
    IReadOnlyList<string> TechnologyLines,
    IReadOnlyList<string> DescriptionLines);

public sealed record RelationshipView(
    string Id,
    string From,
    string To,
    string Label,
    RelationshipDirection Direction,
    Line Line,
    IReadOnlyList<string> LabelLines);

/// <summary>The context diagram laid out for drawing. The screen and the SVG export both draw from this.</summary>
public sealed record ContextView(
    string Title,
    IReadOnlyList<ElementView> Elements,
    IReadOnlyList<RelationshipView> Relationships,
    Rect Bounds)
{
    public ElementView? Element(string id) => Elements.FirstOrDefault(e => e.Id == id);

    public RelationshipView? RelationshipOf(string elementId) =>
        Relationships.FirstOrDefault(r => r.From == elementId || r.To == elementId);

    public static ContextView Of(ContextDiagram diagram, string title)
    {
        var elements = diagram.Elements.Select(ElementOf).ToList();
        var byId = diagram.Elements.ToDictionary(e => e.Id);
        var relationships = diagram.Relationships
            .Where(r => byId.ContainsKey(r.From) && byId.ContainsKey(r.To))
            .Select(r =>
            {
                var line = LineBetween(byId[r.From], byId[r.To]);
                return new RelationshipView(r.Id, r.From, r.To, r.Label, r.Direction, line,
                    TextWrap.LabelLines(r.Label, line.Length));
            }).ToList();
        var bounds = elements.Select(e => e.Box).Aggregate(Rect.Union).Inflate(DiagramStyle.ExportMargin);
        return new ContextView(title, elements, relationships, bounds);
    }

    private static ElementView ElementOf(Element e)
    {
        var box = ContextLayout.BoundsOf(e);
        var system = e.Type == ElementType.System;
        var textWidth = box.Width - 20;
        var nameSize = system ? DiagramStyle.SystemNameFontSize : DiagramStyle.NameFontSize;
        // Technology gets two lines, and the system's description three.
        return new ElementView(e.Id, e.Type, e.Name, e.Description, e.Technology, e.Pinned, box,
            TextWrap.Wrap(e.Name, textWidth, nameSize, true, 2),
            system ? [] : TextWrap.Wrap(e.Technology, textWidth, DiagramStyle.SmallFontSize, false, 2),
            system ? TextWrap.Wrap(e.Description, textWidth, DiagramStyle.SmallFontSize, false, 3) : []);
    }

    /// <summary>The visible line for a relationship, clipped to both element edges.</summary>
    public static Line LineBetween(Element from, Element to)
    {
        var a = new Point(from.X, from.Y);
        var b = new Point(to.X, to.Y);
        return new Line(Clip(a, ContextLayout.SizeOf(from.Type), b), Clip(b, ContextLayout.SizeOf(to.Type), a));
    }

    /// <summary>Where the line from a rectangle's centre towards <paramref name="toward"/> leaves the rectangle.</summary>
    public static Point Clip(Point centre, Size size, Point toward)
    {
        var dx = toward.X - centre.X;
        var dy = toward.Y - centre.Y;
        if (dx == 0 && dy == 0) return centre;
        var scaleX = dx == 0 ? double.PositiveInfinity : size.Width / 2 / Math.Abs(dx);
        var scaleY = dy == 0 ? double.PositiveInfinity : size.Height / 2 / Math.Abs(dy);
        var scale = Math.Min(scaleX, scaleY);
        return new Point(centre.X + dx * scale, centre.Y + dy * scale);
    }
}

/// <summary>Draws a <see cref="ContextView"/>: Context Sketch's shapes and slate palette.</summary>
public static class ContextScene
{
    public const string ArrowMarker = "arrow";

    public static Scene Of(ContextView view)
    {
        var shapes = new List<Shape>();
        // Relationships underneath, elements on top.
        shapes.AddRange(view.Relationships.Select(Relationship));
        shapes.AddRange(view.Elements.Select(Element));
        return new Scene(view.Title, view.Bounds, shapes);
    }

    private static Shape Relationship(RelationshipView r)
    {
        var (start, end) = (r.Line.From, r.Line.To);
        var children = new List<Shape>
        {
            // A wide invisible line makes thin arrows easy to click on screen.
            Draw.LineOf(r.Line, "transparent", 14, @class: "grip"),
            Draw.LineOf(r.Line, "$Line", 1.5, @class: "outline", extra:
                r.Direction == RelationshipDirection.TwoWay
                    ? [("marker-end", $"url(#{ArrowMarker})"), ("marker-start", $"url(#{ArrowMarker})")]
                    : [("marker-end", $"url(#{ArrowMarker})")]),
        };

        // Centre the label block on the line, each row on a background patch so the line doesn't strike through it.
        var mid = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
        var rowHeight = TextWrap.LabelFontSize * DiagramStyle.LineHeight;
        var y = mid.Y - r.LabelLines.Count * rowHeight / 2;
        foreach (var text in r.LabelLines)
        {
            var width = TextWrap.Width(text, TextWrap.LabelFontSize) + 8;
            children.Add(Draw.Rect(new Rect(mid.X - width / 2, y, width, rowHeight), "$Background", "none", 0));
            children.Add(Draw.Text(text, new Point(mid.X, y + rowHeight / 2), TextWrap.LabelFontSize, "$LabelText"));
            y += rowHeight;
        }
        return Draw.Group($"relationship:{r.Id}", children, ("class", "relationship"), ("data-id", r.Id));
    }

    private static (string Fill, string Stroke, string Text) Colours(ElementType type) => type switch
    {
        ElementType.System => ("$SystemFill", "$SystemStroke", "$SystemText"),
        ElementType.PrimaryActor => ("$PrimaryFill", "$PrimaryStroke", "$PrimaryText"),
        ElementType.SecondaryActor => ("$SecondaryFill", "$SecondaryStroke", "$SecondaryText"),
        ElementType.Storage => ("$StorageFill", "$StorageStroke", "$StorageText"),
        _ => ("$ExternalFill", "$ExternalStroke", "$ExternalText"),
    };

    private static Shape Element(ElementView e)
    {
        var box = e.Box;
        var (fill, stroke, ink) = Colours(e.Type);
        var children = new List<Shape>
        {
            e.Type switch
            {
                ElementType.Storage => Cylinder(box, fill, stroke),
                ElementType.PrimaryActor => Draw.Rect(box, fill, stroke, DiagramStyle.ElementStrokeWidth, 10, @class: "outline"),
                ElementType.SecondaryActor => Draw.Rect(box, fill, stroke, DiagramStyle.ElementStrokeWidth, 10, "4 3", "outline"),
                ElementType.System => Draw.Rect(box, fill, stroke, DiagramStyle.SystemStrokeWidth, 6, @class: "outline"),
                _ => Draw.Rect(box, fill, stroke, DiagramStyle.ElementStrokeWidth, @class: "outline"),
            },
        };

        var isActor = e.Type is ElementType.PrimaryActor or ElementType.SecondaryActor;
        var nameSize = e.Type == ElementType.System ? DiagramStyle.SystemNameFontSize : DiagramStyle.NameFontSize;
        var small = DiagramStyle.SmallFontSize;
        var lh = DiagramStyle.LineHeight;
        var total = (isActor ? DiagramStyle.GlyphSize : 0)
                    + e.NameLines.Count * nameSize * lh
                    + e.TechnologyLines.Count * small * lh
                    + (e.DescriptionLines.Count > 0 ? 4 + e.DescriptionLines.Count * small * lh : 0);

        // The cylinder's lid takes space at the top, so its text sits a little lower.
        var y = box.Centre.Y + (e.Type == ElementType.Storage ? 6 : 0) - total / 2;
        var cx = box.Centre.X;

        if (isActor)
        {
            children.Add(Draw.Node("circle", ("cx", cx), ("cy", y + 5.5), ("r", 5.5), ("fill", ink)));
            children.Add(Draw.Node("path",
                ("d", $"M{Draw.N(cx - 13)},{Draw.N(y + DiagramStyle.GlyphSize)} A13,13 0 0 1 {Draw.N(cx + 13)},{Draw.N(y + DiagramStyle.GlyphSize)} Z"),
                ("fill", ink)));
            y += DiagramStyle.GlyphSize;
        }
        foreach (var line in e.NameLines)
        {
            children.Add(Draw.Text(line, new Point(cx, y + nameSize * lh / 2), nameSize, ink, bold: true));
            y += nameSize * lh;
        }
        foreach (var line in e.TechnologyLines)
        {
            children.Add(Draw.Text(line, new Point(cx, y + small * lh / 2), small, ink, extra: ("fill-opacity", DiagramStyle.TechnologyOpacity)));
            y += small * lh;
        }
        if (e.DescriptionLines.Count > 0)
        {
            y += 4;
            foreach (var line in e.DescriptionLines)
            {
                children.Add(Draw.Text(line, new Point(cx, y + small * lh / 2), small, ink));
                y += small * lh;
            }
        }
        return Draw.Group($"element:{e.Id}", children, ("class", "element"), ("data-id", e.Id), ("data-type", Camel(e.Type)));
    }

    /// <summary>A cylinder: body plus the front edge of the lid.</summary>
    private static Shape Cylinder(Rect box, string fill, string stroke)
    {
        const double lid = 12;
        string N(double v) => Draw.N(v);
        var rx = box.Width / 2;
        var (l, r) = (box.X, box.Right);
        var body = $"M{N(l)},{N(box.Y + lid)} A{N(rx)},{N(lid)} 0 0 1 {N(r)},{N(box.Y + lid)} L{N(r)},{N(box.Bottom - lid)} A{N(rx)},{N(lid)} 0 0 1 {N(l)},{N(box.Bottom - lid)} Z";
        var rim = $"M{N(l)},{N(box.Y + lid)} A{N(rx)},{N(lid)} 0 0 0 {N(r)},{N(box.Y + lid)}";
        return Draw.Group(null,
        [
            Draw.Node("path", ("d", body), ("fill", fill), ("stroke", stroke), ("stroke-width", 2), ("class", "outline")),
            Draw.Node("path", ("d", rim), ("fill", "none"), ("stroke", stroke), ("stroke-width", 2)),
        ]);
    }

    public static string Camel<T>(T value) where T : struct, Enum
    {
        var name = value.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
