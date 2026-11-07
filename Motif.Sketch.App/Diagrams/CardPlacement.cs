namespace Motif.Sketch.App;

/// <summary>
/// Decision 28: the properties card opens in whichever of the pane's four corners covers least of the
/// selected item (and of the other floating cards), preferring the farthest corner, and stays in its
/// last corner while that corner covers nothing, so it doesn't jump on every click.
/// </summary>
public static class CardPlacement
{
    public const double Margin = 12;
    public static readonly string[] Corners = ["tr", "br", "tl", "bl"];

    public static string Choose(Rect item, double canvasWidth, double canvasHeight, double cardWidth, double cardHeight,
        IReadOnlyList<Rect> avoid, string? last)
    {
        Rect At(string corner) => new(
            corner[1] == 'l' ? Margin : canvasWidth - Margin - cardWidth,
            corner[0] == 't' ? Margin : canvasHeight - Margin - cardHeight,
            cardWidth, cardHeight);

        double Covered(string corner)
        {
            var card = At(corner);
            return Overlap(card, item) * 4 + avoid.Sum(a => Overlap(card, a));
        }

        if (last is not null && Covered(last) == 0) return last;
        var centre = item.Centre;
        return Corners
            .OrderBy(Covered)
            .ThenByDescending(c => Distance(At(c).Centre, centre))
            .First();
    }

    public static string Style(string corner) =>
        $"{(corner[0] == 't' ? "top" : "bottom")}: {Margin}px; {(corner[1] == 'l' ? "left" : "right")}: {Margin}px;";

    private static double Overlap(Rect a, Rect b)
    {
        var w = Math.Min(a.Right, b.Right) - Math.Max(a.X, b.X);
        var h = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Y, b.Y);
        return w > 0 && h > 0 ? w * h : 0;
    }

    private static double Distance(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
