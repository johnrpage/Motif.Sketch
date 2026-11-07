namespace Motif.Sketch;

/// <summary>
/// The one table of diagram style: sizes, strokes, fonts and the two palettes. Exports always use
/// <see cref="Light"/>. On screen the app turns both palettes into CSS custom properties, so dark
/// mode swaps in <see cref="Dark"/> without changing a single shape.
/// </summary>
public static class DiagramStyle
{
    public const string FontFamily = "'IBM Plex Sans', 'Segoe UI', Arial, sans-serif";
    public const double LineHeight = 1.25;
    public const double TechnologyOpacity = 0.8;

    // Context diagram shapes.
    public static readonly Size SystemSize = new(240, 130);
    public static readonly Size ElementSize = new(170, 90);
    public const double SystemStrokeWidth = 3;
    public const double ElementStrokeWidth = 2;
    public const double NameFontSize = 14;
    public const double SystemNameFontSize = 16;
    public const double SmallFontSize = 11;
    public const double GlyphSize = 26;
    public const double ExportMargin = 40;

    // Sketch shapes.
    public const double AdapterNameFontSize = 13;
    public const double AdapterCaptionFontSize = 11;
    public const double PortNameFontSize = 14;
    public const double SketchTitleFontSize = 18;

    /// <summary>The export palette, which is also the light screen palette.</summary>
    public static readonly IReadOnlyDictionary<string, string> Light = new Dictionary<string, string>
    {
        ["Background"] = "#FFFFFF",
        ["SystemFill"] = "#FFFFFF", ["SystemStroke"] = "#0F172A", ["SystemText"] = "#0F172A",
        ["PrimaryFill"] = "#64748B", ["PrimaryStroke"] = "#475569", ["PrimaryText"] = "#FFFFFF",
        ["SecondaryFill"] = "#F8FAFC", ["SecondaryStroke"] = "#64748B", ["SecondaryText"] = "#334155",
        ["ExternalFill"] = "#E2E8F0", ["ExternalStroke"] = "#94A3B8", ["ExternalText"] = "#1E293B",
        ["StorageFill"] = "#CBD5E1", ["StorageStroke"] = "#475569", ["StorageText"] = "#1E293B",
        ["Line"] = "#52606D", ["LabelText"] = "#0F172A",

        ["Ink"] = "#0F172A", ["HexagonFill"] = "#F8FAFC", ["CoreFill"] = "#FFFFFF",
        ["UserFill"] = "#FDECEA", ["UserInk"] = "#B03A2E",
        ["PortFill"] = "#FFFFFF", ["PortInk"] = "#334155", ["PortText"] = "#334155",
        ["RealFill"] = "#64748B", ["RealStroke"] = "#475569", ["RealText"] = "#FFFFFF", ["RealCaption"] = "#E2E8F0",
        ["StandInFill"] = "#FFFFFF", ["StandInStroke"] = "#64748B", ["StandInText"] = "#0F172A", ["StandInCaption"] = "#64748B",
        ["ChecksFill"] = "#E2E8F0", ["ChecksStroke"] = "#64748B", ["ChecksText"] = "#0F172A", ["ChecksCaption"] = "#475569",
        ["Muted"] = "#94A3B8",

        // On screen only.
        ["Divider"] = "#CBD5E1", ["Selection"] = "#EA580C", ["Warning"] = "#EA580C",
    };

    /// <summary>The dark twin, used on screen only.</summary>
    public static readonly IReadOnlyDictionary<string, string> Dark = new Dictionary<string, string>
    {
        ["Background"] = "#141C2B",
        ["SystemFill"] = "#1B2638", ["SystemStroke"] = "#E2E8F0", ["SystemText"] = "#E2E8F0",
        ["PrimaryFill"] = "#4D5D73", ["PrimaryStroke"] = "#8796AD", ["PrimaryText"] = "#FFFFFF",
        ["SecondaryFill"] = "#1B2638", ["SecondaryStroke"] = "#8796AD", ["SecondaryText"] = "#E2E8F0",
        ["ExternalFill"] = "#2B3A50", ["ExternalStroke"] = "#7C8BA1", ["ExternalText"] = "#E2E8F0",
        ["StorageFill"] = "#2B3A50", ["StorageStroke"] = "#8796AD", ["StorageText"] = "#E2E8F0",
        ["Line"] = "#8796AD", ["LabelText"] = "#E2E8F0",

        ["Ink"] = "#E2E8F0", ["HexagonFill"] = "#19243A", ["CoreFill"] = "#1B2638",
        ["UserFill"] = "#3A1F22", ["UserInk"] = "#F0857A",
        ["PortFill"] = "#1B2638", ["PortInk"] = "#8796AD", ["PortText"] = "#E2E8F0",
        ["RealFill"] = "#4D5D73", ["RealStroke"] = "#7C8BA1", ["RealText"] = "#FFFFFF", ["RealCaption"] = "#E2E8F0",
        ["StandInFill"] = "#1B2638", ["StandInStroke"] = "#8796AD", ["StandInText"] = "#E2E8F0", ["StandInCaption"] = "#94A3B8",
        ["ChecksFill"] = "#2B3A50", ["ChecksStroke"] = "#8796AD", ["ChecksText"] = "#E2E8F0", ["ChecksCaption"] = "#94A3B8",
        ["Muted"] = "#2E3B52",

        ["Divider"] = "#2E3B52", ["Selection"] = "#FB923C", ["Warning"] = "#FB923C",
    };

    /// <summary>CSS custom properties for both palettes: <c>--dg-Name</c>, light by default, dark under the dark theme.</summary>
    public static string Css()
    {
        static string Vars(IReadOnlyDictionary<string, string> palette) =>
            string.Join(" ", palette.Select(p => $"--dg-{p.Key}: {p.Value};"));
        var dark = Vars(Dark);
        return $":root {{ {Vars(Light)} }}\n" +
               $"@media (prefers-color-scheme: dark) {{ :root:not([data-theme=\"light\"]) {{ {dark} }} }}\n" +
               $":root[data-theme=\"dark\"] {{ {dark} }}\n";
    }
}
