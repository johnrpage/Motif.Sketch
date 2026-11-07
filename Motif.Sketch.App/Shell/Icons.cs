using Microsoft.AspNetCore.Components;

namespace Motif.Sketch.App;

/// <summary>The app's few icons, drawn as inline SVG in the mockup's style.</summary>
public static class Icons
{
    private static MarkupString I(string paths, string extra = "") =>
        new($"<svg class=\"i\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"{extra}>{paths}</svg>");

    public static readonly MarkupString Mark = new(
        "<svg class=\"mark\" viewBox=\"0 0 24 24\" aria-hidden=\"true\">" +
        "<rect x=\"2.5\" y=\"13.5\" width=\"7\" height=\"7\" rx=\"1.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\"/>" +
        "<rect x=\"8.5\" y=\"8.5\" width=\"7\" height=\"7\" rx=\"1.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\"/>" +
        "<rect x=\"14.5\" y=\"3.5\" width=\"7\" height=\"7\" rx=\"1.5\" fill=\"var(--accent)\"/></svg>");

    public static readonly MarkupString Chevron = I("<path d=\"M6 9l6 6 6-6\"/>", " style=\"width:12px\"");
    public static readonly MarkupString Save = I("<path d=\"M5 3h11l3 3v15H5z\"/><path d=\"M8 3v5h7V3M8 21v-7h8v7\"/>");
    public static readonly MarkupString Undo = I("<path d=\"M9 14L4 9l5-5\"/><path d=\"M4 9h10a6 6 0 010 12h-3\"/>");
    public static readonly MarkupString Redo = I("<path d=\"M15 14l5-5-5-5\"/><path d=\"M20 9H10a6 6 0 000 12h3\"/>");
    public static readonly MarkupString Export = I("<path d=\"M12 3v12M7 10l5 5 5-5M4 21h16\"/>");
    public static readonly MarkupString Link = I("<path d=\"M10 14a5 5 0 007 0l3-3a5 5 0 00-7-7l-1 1\"/><path d=\"M14 10a5 5 0 00-7 0l-3 3a5 5 0 007 7l1-1\"/>");
    public static readonly MarkupString Copy = I("<rect x=\"8\" y=\"8\" width=\"13\" height=\"13\" rx=\"2\"/><path d=\"M4 16V5a2 2 0 012-2h11\"/>");
    public static readonly MarkupString Eye = I("<path d=\"M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>");
    public static readonly MarkupString Help = I("<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M9.5 9.5a2.5 2.5 0 015 0c0 1.7-2.5 2-2.5 4M12 17h.01\"/>");
    public static readonly MarkupString Sun = I("<circle cx=\"12\" cy=\"12\" r=\"4\"/><path d=\"M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4\"/>");
    public static readonly MarkupString Moon = I("<path d=\"M20 14.5A8 8 0 019.5 4a8 8 0 1010.5 10.5z\"/>");
    public static readonly MarkupString Monitor = I("<rect x=\"3\" y=\"4\" width=\"18\" height=\"12\" rx=\"2\"/><path d=\"M8 20h8M12 16v4\"/>");
    public static readonly MarkupString Fit = I("<path d=\"M4 9V4h5M20 9V4h-5M4 15v5h5M20 15v5h-5\"/>");
    public static readonly MarkupString Maximise = I("<path d=\"M14 4h6v6M10 20H4v-6M20 4l-7 7M4 20l7-7\"/>");
    public static readonly MarkupString Grid = I("<rect x=\"3\" y=\"3\" width=\"7\" height=\"7\" rx=\"1.5\"/><rect x=\"14\" y=\"3\" width=\"7\" height=\"7\" rx=\"1.5\"/><rect x=\"3\" y=\"14\" width=\"7\" height=\"7\" rx=\"1.5\"/><rect x=\"14\" y=\"14\" width=\"7\" height=\"7\" rx=\"1.5\"/>");
    public static readonly MarkupString Unpin = I("<path d=\"M12 17v5M9 3h6l-1 6 3 3v2H7v-2l3-3z\"/>");
    public static readonly MarkupString Trash = I("<path d=\"M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3\"/>");
    public static readonly MarkupString Arrow = I("<path d=\"M5 12h14M13 6l6 6-6 6\"/>");
    public static readonly MarkupString Close = new("<span aria-hidden=\"true\">✕</span>");

    public static MarkupString Glyph(ElementType type) => new(type switch
    {
        ElementType.PrimaryActor => "<svg class=\"glyph\" viewBox=\"0 0 16 12\"><rect x=\"1\" y=\"1\" width=\"14\" height=\"10\" rx=\"2.5\" style=\"fill:var(--dg-PrimaryFill)\"/></svg>",
        ElementType.SecondaryActor => "<svg class=\"glyph\" viewBox=\"0 0 16 12\"><rect x=\"1\" y=\"1\" width=\"14\" height=\"10\" rx=\"2.5\" stroke-dasharray=\"2 1.5\" style=\"fill:var(--dg-SecondaryFill);stroke:var(--dg-SecondaryStroke)\"/></svg>",
        ElementType.ExternalSystem => "<svg class=\"glyph\" viewBox=\"0 0 16 12\"><rect x=\"1\" y=\"1\" width=\"14\" height=\"10\" style=\"fill:var(--dg-ExternalFill);stroke:var(--dg-SecondaryStroke)\"/></svg>",
        ElementType.Storage => "<svg class=\"glyph\" viewBox=\"0 0 16 12\"><path d=\"M2 3v6c0 1.1 2.7 2 6 2s6-.9 6-2V3\" style=\"fill:var(--dg-StorageFill);stroke:var(--dg-SecondaryStroke)\"/><ellipse cx=\"8\" cy=\"3\" rx=\"6\" ry=\"2\" style=\"fill:var(--dg-StorageFill);stroke:var(--dg-SecondaryStroke)\"/></svg>",
        _ => "<svg class=\"glyph\" viewBox=\"0 0 16 12\"><rect x=\"1\" y=\"1\" width=\"14\" height=\"10\" rx=\"1.5\" style=\"fill:var(--dg-SystemFill);stroke:var(--dg-SystemStroke)\" stroke-width=\"1.6\"/></svg>",
    });

    public static readonly MarkupString PortGlyph = new(
        "<svg class=\"glyph\" viewBox=\"0 0 16 12\"><circle cx=\"8\" cy=\"6\" r=\"4.5\" fill=\"none\" style=\"stroke:var(--dg-PortInk)\" stroke-width=\"1.5\"/></svg>");

    public static readonly MarkupString AdapterGlyph = new(
        "<svg class=\"glyph\" viewBox=\"0 0 16 12\"><rect x=\"1\" y=\"2\" width=\"14\" height=\"8\" rx=\"1.5\" style=\"fill:var(--dg-RealFill)\"/></svg>");

    public static string TypeName(ElementType type) => type switch
    {
        ElementType.System => "System",
        ElementType.PrimaryActor => "Primary actor",
        ElementType.SecondaryActor => "Secondary actor",
        ElementType.ExternalSystem => "External system",
        _ => "Storage",
    };
}
