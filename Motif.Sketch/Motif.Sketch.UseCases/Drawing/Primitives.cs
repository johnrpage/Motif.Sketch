namespace Motif.Sketch;

/// <summary>A point in diagram space. y grows downwards.</summary>
public readonly record struct Point(double X, double Y);

public readonly record struct Size(double Width, double Height);

public readonly record struct Rect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public Point Centre => new(X + Width / 2, Y + Height / 2);

    public static Rect Around(Point centre, Size size) =>
        new(centre.X - size.Width / 2, centre.Y - size.Height / 2, size.Width, size.Height);

    public Rect Inflate(double amount) => new(X - amount, Y - amount, Width + 2 * amount, Height + 2 * amount);

    /// <summary>True when the interiors overlap; rectangles that merely touch don't intersect.</summary>
    public bool Intersects(Rect other) => X < other.Right && other.X < Right && Y < other.Bottom && other.Y < Bottom;

    public static Rect Union(Rect a, Rect b)
    {
        var x = Math.Min(a.X, b.X);
        var y = Math.Min(a.Y, b.Y);
        return new Rect(x, y, Math.Max(a.Right, b.Right) - x, Math.Max(a.Bottom, b.Bottom) - y);
    }

    public static Rect Bound(IEnumerable<Point> points)
    {
        var list = points.ToList();
        var minX = list.Min(p => p.X);
        var minY = list.Min(p => p.Y);
        return new Rect(minX, minY, list.Max(p => p.X) - minX, list.Max(p => p.Y) - minY);
    }
}

public sealed record Line(Point From, Point To)
{
    public double Length => Math.Sqrt((To.X - From.X) * (To.X - From.X) + (To.Y - From.Y) * (To.Y - From.Y));
    public Point Midpoint => new((From.X + To.X) / 2, (From.Y + To.Y) / 2);
}
