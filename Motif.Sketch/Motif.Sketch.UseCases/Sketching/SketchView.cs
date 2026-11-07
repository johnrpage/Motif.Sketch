namespace Motif.Sketch;

/// <summary>The hexagon: Domain in the middle, Tasks and Queries between it and the User port.</summary>
public sealed record HexagonShape(
    IReadOnlyList<Point> Corners,
    Point DomainCentre,
    double DomainRadius,
    IReadOnlyList<Point> TasksRegion,
    IReadOnlyList<Point> QueriesRegion,
    Point TasksLabel,
    Point QueriesLabel,
    Line TasksQueriesDivider);

/// <summary>A context diagram element an adapter stands for.</summary>
public sealed record SourceView(string ElementId, string Name, ElementType Type);

public sealed record AdapterShape(
    string Id,
    string PortId,
    string Name,
    string? Technology,
    string? Description,
    AdapterKind Kind,
    IReadOnlyList<SourceView> Sources,
    bool SourceDeleted,
    string? Caption,
    Rect Box,
    Line Link);

/// <summary>Which end of the label text sits on the label point.</summary>
public enum LabelAlign { Start, Middle, End }

/// <summary>A port on its stalk. The label point is the vertical middle of the name text.</summary>
public sealed record PortShape(
    string Id,
    string Name,
    string? Description,
    Side Side,
    bool IsUser,
    Point Centre,
    double Radius,
    Line Stalk,
    Point Label,
    LabelAlign LabelAlign,
    IReadOnlyList<AdapterShape> Adapters);

/// <summary>The sketch laid out for drawing. The screen and the SVG export both draw from this.</summary>
public sealed record SketchView(
    string Title,
    string SystemId,
    HexagonShape Hexagon,
    Line CentreLine,
    IReadOnlyList<PortShape> Ports,
    Rect Bounds)
{
    public PortShape? Port(string id) => Ports.FirstOrDefault(p => p.Id == id);

    public AdapterShape? Adapter(string id) => Ports.SelectMany(p => p.Adapters).FirstOrDefault(a => a.Id == id);
}

/// <summary>An element still waiting to be placed in the sketch.</summary>
public sealed record UnplacedItem(string ElementId, string Name, ElementType Type, string? Technology);

/// <summary>
/// Lays a sketch out: a pointy-topped hexagon at the origin, ports placed radially around it on
/// stalks (driving on the left with the User port at nine o'clock, driven on the right), and each
/// port's adapters in a column beside it. The same sketch always gives the same layout.
/// </summary>
public static class SketchLayout
{
    public const double HexagonRadius = 170;
    public const double DomainRadius = 72;
    public const double PortRadius = 20;
    public const double UserPortRadius = 24;
    public const double StalkLength = 55;
    public const double AdapterHeight = 50;
    public const double AdapterGap = 10;
    public const double MaxSpread = 75;
    public const double PortSpacing = 50;

    private const double TasksFrom = 130, Divider = 180, QueriesTo = 230;
    private static readonly double Apothem = HexagonRadius * Math.Cos(Radians(30));

    public static SketchView Of(Project project)
    {
        var sketch = project.Sketch ?? throw new InvalidOperationException("There is no sketch.");
        var system = project.ContextDiagram.System;
        var hexagon = Hexagon();
        var ports = new List<PortShape>();
        foreach (var (port, angle) in Angles(sketch))
            ports.Add(PortAt(project.ContextDiagram, port, angle));
        foreach (var side in new[] { Side.Driving, Side.Driven })
            SeparateColumns(ports, side);

        var content = Rect.Bound(hexagon.Corners.Concat(ports.SelectMany(PortExtent)));
        const double titleSpace = 40;
        var bounds = new Rect(content.X - 20, content.Y - titleSpace - 20, content.Width + 40, content.Height + titleSpace + 40);
        var titleWidth = TextWrap.Width(system.Name, DiagramStyle.SketchTitleFontSize, true);
        if (bounds.Width < titleWidth + 40) bounds = bounds with { Width = titleWidth + 40 };
        var centreLine = new Line(new(0, bounds.Y), new(0, bounds.Bottom));
        return new SketchView(system.Name, system.Id, hexagon, centreLine, ports, bounds);
    }

    public static double AdapterWidth(string name, string? caption) =>
        Math.Clamp(Math.Max(TextWrap.Width(name, DiagramStyle.AdapterNameFontSize, true),
            TextWrap.Width(caption ?? "", DiagramStyle.AdapterCaptionFontSize)) + 32, 100, 230);

    /// <summary>The second line of an adapter box: its technology, and its kind if it isn't Real.</summary>
    public static string? CaptionFor(Adapter adapter) => adapter.Kind switch
    {
        AdapterKind.Real => adapter.Technology,
        _ when adapter.Technology is null => adapter.Kind.ToString(),
        _ => $"{adapter.Kind} · {adapter.Technology}",
    };

    private static HexagonShape Hexagon()
    {
        var corners = Enumerable.Range(0, 6).Select(i => Polar(90 + 60 * i, HexagonRadius)).ToList();
        var middle = (DomainRadius + Apothem) / 2;
        return new HexagonShape(
            corners,
            new(0, 0),
            DomainRadius,
            Region(TasksFrom, Divider),
            Region(Divider, QueriesTo),
            Polar((TasksFrom + Divider) / 2 + 3, middle),
            Polar((Divider + QueriesTo) / 2 - 3, middle),
            new Line(Polar(Divider, DomainRadius), Polar(Divider, EdgeDistance(Divider))));
    }

    /// <summary>The ring segment between the Domain circle and the hexagon's edge, between two angles.</summary>
    private static List<Point> Region(double from, double to)
    {
        var points = new List<Point> { Polar(from, EdgeDistance(from)) };
        for (var corner = 30.0; corner < 390; corner += 60)
            if (corner > from && corner < to) points.Add(Polar(corner, HexagonRadius));
        points.Add(Polar(to, EdgeDistance(to)));
        for (var a = to; a >= from; a -= 5)
            points.Add(Polar(a, DomainRadius));
        return points;
    }

    private static IEnumerable<(Port Port, double Angle)> Angles(Sketch sketch)
    {
        // Driving: the User port at nine o'clock, any others alternating above and below it.
        yield return (sketch.UserPort, 180);
        var others = sketch.PortsOn(Side.Driving).Where(p => !p.IsUser).ToList();
        var above = (others.Count + 1) / 2;
        var drivingStep = above == 0 ? 0 : Math.Min(PortSpacing, MaxSpread / above);
        for (var j = 0; j < others.Count; j++)
        {
            var k = j / 2 + 1;
            yield return (others[j], j % 2 == 0 ? 180 - k * drivingStep : 180 + k * drivingStep);
        }

        // Driven: spread evenly across the right, top to bottom, centred on three o'clock.
        var driven = sketch.PortsOn(Side.Driven).ToList();
        var step = driven.Count > 1 ? Math.Min(PortSpacing, 2 * MaxSpread / (driven.Count - 1)) : 0;
        for (var i = 0; i < driven.Count; i++)
            yield return (driven[i], step * (driven.Count - 1) / 2 - i * step);
    }

    private static PortShape PortAt(ContextDiagram diagram, Port port, double angle)
    {
        var radius = port.IsUser ? UserPortRadius : PortRadius;
        var edge = EdgeDistance(angle);
        var centre = Polar(angle, edge + StalkLength + radius);
        var stalk = new Line(Polar(angle, edge), Polar(angle, edge + StalkLength));

        var right = port.Side == Side.Driven;
        var outward = Polar(angle, radius + 30);
        var anchor = new Point(centre.X + outward.X, centre.Y + outward.Y);
        var count = port.Adapters.Count;
        var columnHeight = count * AdapterHeight + Math.Max(0, count - 1) * AdapterGap;

        var adapters = new List<AdapterShape>();
        for (var i = 0; i < count; i++)
        {
            var adapter = port.Adapters[i];
            var caption = CaptionFor(adapter);
            var width = AdapterWidth(adapter.Name, caption);
            var y = anchor.Y - columnHeight / 2 + i * (AdapterHeight + AdapterGap);
            var x = right ? anchor.X + 10 : anchor.X - 10 - width;
            var box = new Rect(x, y, width, AdapterHeight);
            var target = new Point(right ? box.X : box.Right, box.Centre.Y);
            var sources = adapter.Sources
                .Select(diagram.Find).OfType<Element>()
                .Select(e => new SourceView(e.Id, e.Name, e.Type)).ToList();
            adapters.Add(new AdapterShape(adapter.Id, port.Id, adapter.Name, adapter.Technology, adapter.Description,
                adapter.Kind, sources, adapter.SourceDeleted, caption, box,
                new Line(TowardsEdge(centre, target, radius), target)));
        }

        var (label, align) = LabelFor(angle, centre, radius, right);
        return new PortShape(port.Id, port.Name, port.Description, port.Side, port.IsUser, centre, radius, stalk, label, align, adapters);
    }

    /// <summary>
    /// Ports near three or nine o'clock have their name centred above them. Diagonal ports have it on
    /// the side away from both the stalk and the adapters: above for upper ports, below for lower ones.
    /// </summary>
    private static (Point, LabelAlign) LabelFor(double angle, Point centre, double radius, bool right)
    {
        var tilt = right ? angle : 180 - angle;
        const double gap = 16;
        if (Math.Abs(tilt) <= 10)
            return (new(centre.X, centre.Y - radius - gap), LabelAlign.Middle);
        var y = tilt > 0 ? centre.Y - radius - gap : centre.Y + radius + gap;
        return (new(centre.X, y), right ? LabelAlign.End : LabelAlign.Start);
    }

    /// <summary>
    /// Busy neighbouring ports can have overlapping adapter columns. Working outwards from the port
    /// nearest the middle of the side, push each column up or down until it clears its neighbour.
    /// </summary>
    private static void SeparateColumns(List<PortShape> ports, Side side)
    {
        const double gap = 16;
        var onSide = ports.Where(p => p.Side == side).OrderBy(p => p.Centre.Y).ToList();
        if (onSide.Count < 2) return;
        var pivot = onSide.IndexOf(onSide.MinBy(p => Math.Abs(p.Centre.Y))!);

        for (var i = pivot - 1; i >= 0; i--)
        {
            var limit = Top(onSide[i + 1]) - gap;
            var bottom = Bottom(onSide[i]);
            if (bottom > limit) onSide[i] = Shift(onSide[i], limit - bottom);
        }
        for (var i = pivot + 1; i < onSide.Count; i++)
        {
            var limit = Bottom(onSide[i - 1]) + gap;
            var top = Top(onSide[i]);
            if (top < limit) onSide[i] = Shift(onSide[i], limit - top);
        }
        foreach (var shifted in onSide)
            ports[ports.FindIndex(p => p.Id == shifted.Id)] = shifted;

        static double Top(PortShape p) =>
            Math.Min(p.Adapters.Count > 0 ? p.Adapters[0].Box.Y : double.MaxValue, p.Centre.Y - p.Radius - 26);

        static double Bottom(PortShape p) =>
            Math.Max(p.Adapters.Count > 0 ? p.Adapters[^1].Box.Bottom : double.MinValue, p.Centre.Y + p.Radius + 26);
    }

    /// <summary>Moves a port's adapter column up or down, re-aiming the links from the port circle.</summary>
    private static PortShape Shift(PortShape port, double dy) => port with
    {
        Adapters = port.Adapters.Select(a =>
        {
            var target = a.Link.To with { Y = a.Link.To.Y + dy };
            return a with { Box = a.Box with { Y = a.Box.Y + dy }, Link = new Line(TowardsEdge(port.Centre, target, port.Radius), target) };
        }).ToList(),
    };

    private static IEnumerable<Point> PortExtent(PortShape port)
    {
        var r = port.Radius;
        yield return new(port.Centre.X - r, port.Centre.Y - r);
        yield return new(port.Centre.X + r, port.Centre.Y + r);
        var width = TextWrap.Width(port.Name, DiagramStyle.PortNameFontSize, true);
        var left = port.LabelAlign switch
        {
            LabelAlign.Start => port.Label.X,
            LabelAlign.End => port.Label.X - width,
            _ => port.Label.X - width / 2,
        };
        yield return new(left, port.Label.Y - 10);
        yield return new(left + width, port.Label.Y + 10);
        foreach (var a in port.Adapters)
        {
            yield return new(a.Box.X, a.Box.Y);
            yield return new(a.Box.Right, a.Box.Bottom);
        }
    }

    /// <summary>How far from the centre the hexagon's edge is, in a given direction.</summary>
    private static double EdgeDistance(double angle)
    {
        var normal = Math.Round(angle / 60) * 60;
        return Apothem / Math.Cos(Radians(angle - normal));
    }

    private static Point TowardsEdge(Point centre, Point target, double radius)
    {
        var dx = target.X - centre.X;
        var dy = target.Y - centre.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        return length == 0 ? centre : new(centre.X + dx / length * radius, centre.Y + dy / length * radius);
    }

    /// <summary>Angle in degrees, anticlockwise from three o'clock; screen y points down.</summary>
    private static Point Polar(double angle, double distance) =>
        new(distance * Math.Cos(Radians(angle)), -distance * Math.Sin(Radians(angle)));

    private static double Radians(double degrees) => degrees * Math.PI / 180;
}

/// <summary>Draws a <see cref="SketchView"/>: HexaSketch's hexagon, with pale slate Checks adapters.</summary>
public static class SketchScene
{
    public static Scene Of(SketchView view)
    {
        var b = view.Bounds;
        var shapes = new List<Shape>
        {
            Draw.LineOf(view.CentreLine, "$Muted", 1, "6 6", "screen-only"),
            Hexagon(view.Hexagon),
        };
        shapes.AddRange(view.Ports.Select(Port));
        shapes.Add(Draw.Text(view.Title, new Point(b.X + 20, b.Y + 30), DiagramStyle.SketchTitleFontSize, "$Ink", "start", bold: true));
        return new Scene(view.Title, b, shapes);
    }

    private static Shape Hexagon(HexagonShape h) => Draw.Group("hexagon",
    [
        Draw.Polygon(h.Corners, "$HexagonFill", "$Ink", 1.5, "outline"),
        Draw.Polygon(h.TasksRegion, "$UserFill", "$UserInk", 1),
        Draw.Polygon(h.QueriesRegion, "$UserFill", "$UserInk", 1),
        Draw.LineOf(h.TasksQueriesDivider, "$UserInk", 1, "4 3"),
        Draw.Text("Tasks", h.TasksLabel, 13, "$UserInk"),
        Draw.Text("Queries", h.QueriesLabel, 13, "$UserInk"),
        Draw.Circle(h.DomainCentre, h.DomainRadius, "$CoreFill", "$Ink", 1.5),
        Draw.Text("Domain", h.DomainCentre, 16, "$Ink", bold: true),
    ], ("class", "hexagon"));

    private static Shape Port(PortShape port)
    {
        var weight = port.IsUser ? 4.0 : 2.0;
        var anchor = port.LabelAlign switch { LabelAlign.Start => "start", LabelAlign.End => "end", _ => "middle" };
        var portGroup = Draw.Group($"port:{port.Id}",
        [
            Draw.LineOf(port.Stalk, "$Ink", weight),
            Draw.Circle(port.Centre, port.Radius, "$PortFill", "$PortInk", weight, "outline"),
            Draw.Text(port.Name, port.Label, DiagramStyle.PortNameFontSize, "$PortText", anchor, bold: true),
        ], ("class", "port"), ("data-id", port.Id));

        var children = new List<Shape>();
        foreach (var adapter in port.Adapters)
            children.Add(Draw.LineOf(adapter.Link, "$Ink", 1));
        children.Add(portGroup);
        children.AddRange(port.Adapters.Select(Adapter));
        return Draw.Group(null, children);
    }

    private static Shape Adapter(AdapterShape a)
    {
        var (fill, stroke, text, caption, dash) = a.Kind switch
        {
            AdapterKind.Real => ("$RealFill", "$RealStroke", "$RealText", "$RealCaption", (string?)null),
            AdapterKind.Checks => ("$ChecksFill", "$ChecksStroke", "$ChecksText", "$ChecksCaption", null),
            _ => ("$StandInFill", "$StandInStroke", "$StandInText", "$StandInCaption", "5 3"),
        };
        var children = new List<Shape> { Draw.Rect(a.Box, fill, stroke, 1.2, 4, dash, "outline") };
        var c = a.Box.Centre;
        var room = a.Box.Width - 12;
        var name = TextWrap.Fit(a.Name, room, DiagramStyle.AdapterNameFontSize, true);
        if (a.Caption is null)
            children.Add(Draw.Text(name, c, DiagramStyle.AdapterNameFontSize, text, bold: true));
        else
        {
            children.Add(Draw.Text(name, c with { Y = c.Y - 8 }, DiagramStyle.AdapterNameFontSize, text, bold: true));
            children.Add(Draw.Text(TextWrap.Fit(a.Caption, room, DiagramStyle.AdapterCaptionFontSize), c with { Y = c.Y + 9 },
                DiagramStyle.AdapterCaptionFontSize, caption));
        }
        if (a.SourceDeleted)
            children.Add(Draw.Circle(new Point(a.Box.Right - 2, a.Box.Y + 2), 5, "$Warning", "$Background", 1.5, "screen-only flag"));
        return Draw.Group($"adapter:{a.Id}", children, ("class", "adapter"), ("data-id", a.Id), ("data-kind", ContextScene.Camel(a.Kind)));
    }
}
