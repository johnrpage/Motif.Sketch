using System.Xml.Linq;

namespace Motif.Sketch;

/// <summary>
/// Writes a <see cref="Scene"/> as a standalone SVG: cropped to the content, in the light palette,
/// without the screen-only parts (selection, the centre divider, flags).
/// </summary>
public static class SvgWriter
{
    private static readonly XNamespace Ns = "http://www.w3.org/2000/svg";

    public static string Write(Scene scene)
    {
        var b = scene.Bounds;
        var svg = new XElement(Ns + "svg",
            new XAttribute("width", Draw.N(b.Width)),
            new XAttribute("height", Draw.N(b.Height)),
            new XAttribute("viewBox", $"{Draw.N(b.X)} {Draw.N(b.Y)} {Draw.N(b.Width)} {Draw.N(b.Height)}"),
            new XAttribute("font-family", DiagramStyle.FontFamily),
            new XElement(Ns + "title", scene.Title),
            Defs(),
            new XElement(Ns + "rect",
                new XAttribute("x", Draw.N(b.X)), new XAttribute("y", Draw.N(b.Y)),
                new XAttribute("width", Draw.N(b.Width)), new XAttribute("height", Draw.N(b.Height)),
                new XAttribute("fill", DiagramStyle.Light["Background"])));

        foreach (var shape in scene.Shapes.Where(s => !IsScreenOnly(s)))
            svg.Add(Element(shape));

        return new XDeclaration("1.0", "utf-8", null) + "\n" + svg + "\n";
    }

    /// <summary>The colour a paint value stands for in the export palette.</summary>
    public static string Paint(string value) =>
        value.StartsWith('$') ? DiagramStyle.Light[value[1..]] : value;

    public static bool IsScreenOnly(Shape shape) =>
        shape["class"]?.Split(' ').Contains("screen-only") == true;

    private static XElement Defs() =>
        new(Ns + "defs",
            new XElement(Ns + "marker",
                new XAttribute("id", ContextScene.ArrowMarker),
                new XAttribute("viewBox", "0 0 12 10"),
                new XAttribute("markerWidth", "12"), new XAttribute("markerHeight", "10"),
                new XAttribute("refX", "12"), new XAttribute("refY", "5"),
                new XAttribute("orient", "auto-start-reverse"),
                new XAttribute("markerUnits", "userSpaceOnUse"),
                new XElement(Ns + "path", new XAttribute("d", "M0,0 L12,5 L0,10 Z"), new XAttribute("fill", Paint("$Line")))));

    private static XElement Element(Shape shape)
    {
        var element = new XElement(Ns + shape.Tag);
        foreach (var (name, value) in shape.Attributes)
        {
            if (name == "class" && value.Contains("grip")) continue;
            element.Add(new XAttribute(name, Shape.PaintAttributes.Contains(name) ? Paint(value) : value));
        }
        if (shape.Text is not null) element.Add(shape.Text);
        foreach (var child in shape.Children.Where(c => !IsScreenOnly(c) && c["class"] != "grip"))
            element.Add(Element(child));
        return element;
    }
}
