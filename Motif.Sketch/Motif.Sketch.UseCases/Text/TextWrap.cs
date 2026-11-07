namespace Motif.Sketch;

/// <summary>
/// Text measuring and wrapping on IBM Plex Sans's own advance widths, so labels break the same way
/// on screen, in exports and on every platform.
/// </summary>
public static class TextWrap
{
    private static readonly Dictionary<char, (short Regular, short SemiBold)> Widths =
        PlexMetricsTable.Widths.ToDictionary(w => w.Char, w => (w.Regular, w.SemiBold));

    // Characters outside the table (rare in diagram labels) are measured as a wide-ish letter.
    private const short Fallback = 600;

    /// <summary>The width of a line of text in diagram units.</summary>
    public static double Width(string text, double fontSize, bool semiBold = false)
    {
        double units = 0;
        foreach (var c in text)
            units += Widths.TryGetValue(c, out var w) ? (semiBold ? w.SemiBold : w.Regular) : Fallback;
        return units * fontSize / 1000;
    }

    /// <summary>Greedy word wrap into at most <paramref name="maxLines"/>; text that doesn't fit ends in "…".</summary>
    public static List<string> Wrap(string? text, double maxWidth, double fontSize, bool semiBold, int maxLines) =>
        Wrap(text, maxWidth, fontSize, semiBold, maxLines, out _);

    public static List<string> Wrap(string? text, double maxWidth, double fontSize, bool semiBold, int maxLines, out bool cut)
    {
        cut = false;
        if (string.IsNullOrWhiteSpace(text)) return [];

        var lines = new List<string>();
        var current = "";
        foreach (var word in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = current.Length == 0 ? word : current + " " + word;
            if (current.Length == 0 || Width(candidate, fontSize, semiBold) <= maxWidth)
                current = candidate;
            else
            {
                lines.Add(current);
                current = word;
            }
        }
        lines.Add(current);

        if (lines.Count > maxLines)
        {
            lines = lines.Take(maxLines).ToList();
            lines[^1] = Ellipsis(lines[^1] + " …", maxWidth, fontSize, semiBold, force: true);
            cut = true;
        }
        for (var i = 0; i < lines.Count; i++)
        {
            if (Width(lines[i], fontSize, semiBold) <= maxWidth) continue;
            lines[i] = Ellipsis(lines[i], maxWidth, fontSize, semiBold, force: false);
            cut = true;
        }
        return lines;
    }

    /// <summary>One line, cut with "…" if it's too wide.</summary>
    public static string Fit(string text, double maxWidth, double fontSize, bool semiBold = false) =>
        Width(text, fontSize, semiBold) <= maxWidth ? text : Ellipsis(text, maxWidth, fontSize, semiBold, force: false);

    private static string Ellipsis(string text, double maxWidth, double fontSize, bool semiBold, bool force)
    {
        var body = force ? text.TrimEnd('…', ' ') : text;
        if (!force && Width(body, fontSize, semiBold) <= maxWidth) return body;
        while (body.Length > 1 && Width(body.TrimEnd() + "…", fontSize, semiBold) > maxWidth)
            body = body[..^1];
        return body.TrimEnd() + "…";
    }

    // ---- Relationship labels -----------------------------------------------------------------

    public const double LabelFontSize = 12;
    public const int LabelMaxLines = 3;

    // At each end a 12px head plus 20px of visible line, so the arrow still reads as a line either
    // side of the label, plus the label's white patch. Lines are never narrower than about 12
    // characters, however short the arrow, nor wider than about 40.
    private const double LabelInset = 2 * (12 + 20) + 8;
    public const double LabelMinWidth = 80;
    public const double LabelMaxWidth = 264;

    public static double LabelWidthFor(double arrowLength) => Math.Clamp(arrowLength - LabelInset, LabelMinWidth, LabelMaxWidth);

    /// <summary>A relationship label broken into the lines it's drawn on, fitted to its arrow.</summary>
    public static IReadOnlyList<string> LabelLines(string? label, double arrowLength) =>
        Wrap(label, LabelWidthFor(arrowLength), LabelFontSize, false, LabelMaxLines);

    /// <summary>The shortest arrow, from <paramref name="shortest"/> up to <paramref name="longest"/>, whose label fits uncut.</summary>
    public static double ArrowLengthFor(string? label, double shortest, double longest)
    {
        if (string.IsNullOrWhiteSpace(label)) return shortest;
        for (var length = shortest; length < longest; length += 10)
        {
            Wrap(label, LabelWidthFor(length), LabelFontSize, false, LabelMaxLines, out var cut);
            if (!cut) return length;
        }
        return longest;
    }
}
