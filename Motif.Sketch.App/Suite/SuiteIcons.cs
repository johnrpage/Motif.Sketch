// Motif.NET shared web kit: copied from github.com/motif-dotnet/.github/tree/main/brand/web. Don't edit an app's copy.
using Microsoft.AspNetCore.Components;

namespace Motif.Suite;

/// <summary>The icons every Motif tool uses, drawn as inline SVG: 24 × 24, 1.6 stroke, round caps.</summary>
public static class SuiteIcons
{
    /// <summary>An outline icon. Apps draw their own icons with this too, so they match.</summary>
    public static MarkupString Icon(string paths, string extra = "") =>
        new($"<svg class=\"i\" viewBox=\"0 0 24 24\" aria-hidden=\"true\"{extra}>{paths}</svg>");

    /// <summary>The Motif mark in the theme's colours: brand/mark.svg with currentColor and the accent. The splash in index.html has the same copy.</summary>
    public static readonly MarkupString Mark = new(
        "<svg class=\"mark\" viewBox=\"0 0 24 24\" aria-hidden=\"true\">" +
        "<rect x=\"2.9\" y=\"15.74\" width=\"5.36\" height=\"5.36\" rx=\"0.96\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\"/>" +
        "<rect x=\"8.42\" y=\"8.42\" width=\"7.16\" height=\"7.16\" rx=\"1.86\" fill=\"currentColor\"/>" +
        "<rect x=\"14.84\" y=\"2\" width=\"7.16\" height=\"7.16\" rx=\"1.86\" fill=\"var(--accent)\"/></svg>");

    public static readonly MarkupString Chevron = Icon("<path d=\"M6 9l6 6 6-6\"/>", " style=\"width:12px\"");
    public static readonly MarkupString Help = Icon("<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M9.5 9.5a2.5 2.5 0 015 0c0 1.7-2.5 2-2.5 4M12 17h.01\"/>");
    public static readonly MarkupString Sun = Icon("<circle cx=\"12\" cy=\"12\" r=\"4\"/><path d=\"M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4\"/>");
    public static readonly MarkupString Moon = Icon("<path d=\"M20 14.5A8 8 0 019.5 4a8 8 0 1010.5 10.5z\"/>");
    public static readonly MarkupString Monitor = Icon("<rect x=\"3\" y=\"4\" width=\"18\" height=\"12\" rx=\"2\"/><path d=\"M8 20h8M12 16v4\"/>");
    public static readonly MarkupString Link = Icon("<path d=\"M10 14a5 5 0 007 0l3-3a5 5 0 00-7-7l-1 1\"/><path d=\"M14 10a5 5 0 00-7 0l-3 3a5 5 0 007 7l1-1\"/>");
    public static readonly MarkupString Copy = Icon("<rect x=\"8\" y=\"8\" width=\"13\" height=\"13\" rx=\"2\"/><path d=\"M4 16V5a2 2 0 012-2h11\"/>");
    public static readonly MarkupString Close = new("<span aria-hidden=\"true\">✕</span>");
}
