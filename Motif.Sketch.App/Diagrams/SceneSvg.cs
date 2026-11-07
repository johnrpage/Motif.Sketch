using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Motif.Sketch.App;

/// <summary>
/// Draws a <see cref="Scene"/> as inline SVG: the same shapes the export writes, with paint tokens
/// turned into CSS variables so the dark twin applies, plus the screen-only selection and drop marks.
/// </summary>
public sealed class SceneSvg : ComponentBase
{
    private static int instances;
    private readonly string marker = $"arrow-{++instances}";

    [Parameter, EditorRequired] public Scene Scene { get; set; } = default!;

    /// <summary>The viewBox to show; the scene's own bounds if not given.</summary>
    [Parameter] public Rect? View { get; set; }

    [Parameter] public string? Selected { get; set; }
    [Parameter] public string? DropTarget { get; set; }
    [Parameter] public string? Dragging { get; set; }

    /// <summary>A screen-only line down the middle: driving and primary actors on the left, the rest on the right.</summary>
    [Parameter] public bool Divider { get; set; }

    [Parameter] public string? Class { get; set; }
    [Parameter] public Action<ElementReference>? OnElement { get; set; }

    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? Extra { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder b)
    {
        var v = View ?? Scene.Bounds;
        b.OpenElement(0, "svg");
        b.AddAttribute(1, "xmlns", "http://www.w3.org/2000/svg");
        b.AddAttribute(2, "class", "scene " + Class);
        b.AddAttribute(3, "viewBox", $"{Draw(v.X)} {Draw(v.Y)} {Draw(v.Width)} {Draw(v.Height)}");
        b.AddAttribute(4, "preserveAspectRatio", "xMidYMid meet");
        b.AddAttribute(5, "font-family", DiagramStyle.FontFamily);
        if (Extra is not null) b.AddMultipleAttributes(6, Extra);
        if (OnElement is not null) b.AddElementReferenceCapture(7, OnElement);

        b.OpenElement(10, "defs");
        b.OpenElement(11, "marker");
        b.AddAttribute(12, "id", marker);
        b.AddAttribute(13, "viewBox", "0 0 12 10");
        b.AddAttribute(14, "markerWidth", "12");
        b.AddAttribute(15, "markerHeight", "10");
        b.AddAttribute(16, "refX", "12");
        b.AddAttribute(17, "refY", "5");
        b.AddAttribute(18, "orient", "auto-start-reverse");
        b.AddAttribute(19, "markerUnits", "userSpaceOnUse");
        b.OpenElement(20, "path");
        b.AddAttribute(21, "d", "M0,0 L12,5 L0,10 Z");
        b.AddAttribute(22, "style", "fill: var(--dg-Line)");
        b.CloseElement();
        b.CloseElement();
        b.CloseElement();

        if (Divider)
        {
            b.OpenElement(30, "line");
            b.AddAttribute(31, "class", "divider");
            b.AddAttribute(32, "x1", "0");
            b.AddAttribute(33, "x2", "0");
            b.AddAttribute(34, "y1", Draw(v.Y - 10000));
            b.AddAttribute(35, "y2", Draw(v.Bottom + 10000));
            b.CloseElement();
        }

        b.OpenRegion(40);
        foreach (var shape in Scene.Shapes) Render(b, shape);
        b.CloseRegion();
        b.CloseElement();
    }

    private void Render(RenderTreeBuilder b, Shape shape)
    {
        b.OpenElement(0, shape.Tag);
        if (shape.Hit is { } hit) b.SetKey(hit);
        var style = new List<string>();
        var classes = new List<string>();
        foreach (var (name, value) in shape.Attributes)
        {
            if (name == "class") { classes.Add(value); continue; }
            if (Shape.PaintAttributes.Contains(name) && value.StartsWith('$'))
            {
                style.Add($"{name}: var(--dg-{value[1..]})");
                continue;
            }
            b.AddAttribute(1, name, value.Replace("url(#arrow)", $"url(#{marker})"));
        }
        if (shape.Hit is { } h)
        {
            b.AddAttribute(2, "data-hit", h);
            classes.Add("hit");
            if (h == Selected) classes.Add("sel");
            if (h == DropTarget) classes.Add("drop");
            if (h == Dragging) classes.Add("dragging");
        }
        if (classes.Count > 0) b.AddAttribute(3, "class", string.Join(" ", classes));
        if (style.Count > 0) b.AddAttribute(4, "style", string.Join("; ", style));
        if (shape.Text is not null) b.AddContent(5, shape.Text);
        foreach (var child in shape.Children) Render(b, child);
        b.CloseElement();
    }

    private static string Draw(double value) => Motif.Sketch.App.Format.N(value);
}

internal static class Format
{
    public static string N(double value) =>
        Math.Round(value, 2).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
