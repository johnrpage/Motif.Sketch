namespace Motif.Sketch;

/// <summary>The bundled example: Motif Sketch's own design, compiled into the core.</summary>
public static class Example
{
    public const string ResourceName = "Motif.Sketch.example.motifsketch.json";

    /// <summary>The fragment of a link that opens the example read-only, always the current one.</summary>
    public const string Fragment = "example";

    public static bool IsLink(string? fragment) =>
        string.Equals(fragment?.TrimStart('#'), Fragment, StringComparison.OrdinalIgnoreCase);

    public static string Json()
    {
        using var stream = typeof(Example).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The example project is missing from the build.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static Project Project() =>
        ProjectJson.Load(Json()).Project ?? throw new InvalidOperationException("The bundled example is invalid.");
}
