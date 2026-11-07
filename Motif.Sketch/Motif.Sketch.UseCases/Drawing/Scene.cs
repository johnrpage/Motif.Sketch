using System.Globalization;

namespace Motif.Sketch;

/// <summary>
/// One SVG node of a drawn diagram. Paint attributes may name a <see cref="DiagramStyle"/> token as
/// "$Token": the SVG export writes the light colour, and the screen writes a CSS variable so the dark
/// twin applies. <see cref="Hit"/> marks a group the user can select ("element:id", "port:id"...).
/// </summary>
public sealed record Shape(
    string Tag,
    IReadOnlyList<KeyValuePair<string, string>> Attributes,
    IReadOnlyList<Shape> Children,
    string? Text = null,
    string? Hit = null)
{
    /// <summary>Attributes whose value is a paint, and so may hold a token.</summary>
    public static readonly IReadOnlySet<string> PaintAttributes = new HashSet<string> { "fill", "stroke" };

    public string? this[string attribute] => Attributes.FirstOrDefault(a => a.Key == attribute).Value;
}

/// <summary>A whole diagram, drawn: what the screen renders and the SVG export writes.</summary>
/// <param name="Bounds">The content, cropped, with the export margin.</param>
public sealed record Scene(string Title, Rect Bounds, IReadOnlyList<Shape> Shapes)
{
    public IEnumerable<Shape> All() => Shapes.SelectMany(Walk);

    private static IEnumerable<Shape> Walk(Shape s) => s.Children.SelectMany(Walk).Prepend(s);
}

/// <summary>Small builder helpers for scenes, so the drawing code reads like SVG.</summary>
internal static class Draw
{
    public static Shape Group(string? hit, IEnumerable<Shape> children, params (string, object)[] attributes) =>
        new("g", Attrs(attributes), children.ToList(), Hit: hit);

    public static Shape Node(string tag, params (string, object)[] attributes) => new(tag, Attrs(attributes), []);

    public static Shape Text(string text, Point at, double size, string fill, string anchor = "middle", bool bold = false,
        params (string, object)[] extra)
    {
        var attributes = new List<(string, object)>
        {
            ("x", at.X), ("y", at.Y), ("font-size", size), ("fill", fill),
            ("text-anchor", anchor), ("dominant-baseline", "central"),
        };
        if (bold) attributes.Add(("font-weight", "600"));
        attributes.AddRange(extra);
        return new Shape("text", Attrs([.. attributes]), [], text);
    }

    public static Shape Rect(Rect r, string fill, string stroke, double strokeWidth, double radius = 0, string? dash = null,
        string? @class = null)
    {
        var a = new List<(string, object)>
        {
            ("x", r.X), ("y", r.Y), ("width", r.Width), ("height", r.Height),
        };
        if (radius > 0) { a.Add(("rx", radius)); a.Add(("ry", radius)); }
        a.Add(("fill", fill));
        a.Add(("stroke", stroke));
        a.Add(("stroke-width", strokeWidth));
        if (dash is not null) a.Add(("stroke-dasharray", dash));
        if (@class is not null) a.Add(("class", @class));
        return Node("rect", [.. a]);
    }

    public static Shape LineOf(Line line, string stroke, double width, string? dash = null, string? @class = null,
        params (string, object)[] extra)
    {
        var a = new List<(string, object)>
        {
            ("x1", line.From.X), ("y1", line.From.Y), ("x2", line.To.X), ("y2", line.To.Y),
            ("stroke", stroke), ("stroke-width", width), ("stroke-linecap", "round"),
        };
        if (dash is not null) a.Add(("stroke-dasharray", dash));
        if (@class is not null) a.Add(("class", @class));
        a.AddRange(extra);
        return Node("line", [.. a]);
    }

    public static Shape Polygon(IEnumerable<Point> points, string fill, string stroke, double width, string? @class = null)
    {
        var a = new List<(string, object)>
        {
            ("points", string.Join(" ", points.Select(p => $"{N(p.X)},{N(p.Y)}"))),
            ("fill", fill), ("stroke", stroke), ("stroke-width", width), ("stroke-linejoin", "round"),
        };
        if (@class is not null) a.Add(("class", @class));
        return Node("polygon", [.. a]);
    }

    public static Shape Circle(Point c, double r, string fill, string stroke, double width, string? @class = null)
    {
        var a = new List<(string, object)>
            { ("cx", c.X), ("cy", c.Y), ("r", r), ("fill", fill), ("stroke", stroke), ("stroke-width", width) };
        if (@class is not null) a.Add(("class", @class));
        return Node("circle", [.. a]);
    }

    public static IReadOnlyList<KeyValuePair<string, string>> Attrs((string Name, object Value)[] attributes) =>
        attributes.Select(a => new KeyValuePair<string, string>(a.Name, a.Value switch
        {
            double d => N(d),
            int i => i.ToString(CultureInfo.InvariantCulture),
            _ => a.Value.ToString() ?? "",
        })).ToList();

    /// <summary>Numbers to two decimals, invariant, with no "-0".</summary>
    public static string N(double value)
    {
        value = Math.Round(value, 2);
        if (value == 0) value = 0;
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
