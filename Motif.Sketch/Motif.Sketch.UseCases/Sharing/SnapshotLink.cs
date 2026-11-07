using System.IO.Compression;
using System.Text;

namespace Motif.Sketch;

public sealed record SnapshotResult(Project? Project, string? Problem)
{
    public bool Succeeded => Project is not null;
}

/// <summary>
/// Snapshot links: the whole project, minified, raw-deflated and base64url-encoded into the URL
/// fragment as <c>#v1.&lt;data&gt;</c>. Everything after the <c>#</c> stays in the browser.
/// </summary>
public static class SnapshotLink
{
    public const string Prefix = "v1.";

    /// <summary>Links longer than this still work, but some chat and email tools may cut them.</summary>
    public const int LongLinkThreshold = 8000;

    public static string Fragment(Project project)
    {
        var bytes = Encoding.UTF8.GetBytes(ProjectJson.Save(project, indented: false));
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            deflate.Write(bytes);
        return Prefix + Convert.ToBase64String(output.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string Link(string pageUrl, Project project)
    {
        var hash = pageUrl.IndexOf('#');
        return (hash < 0 ? pageUrl : pageUrl[..hash]) + "#" + Fragment(project);
    }

    /// <summary>True if the fragment looks like a snapshot link at all (any version).</summary>
    public static bool IsSnapshot(string? fragment) =>
        fragment is { Length: > 0 } f && System.Text.RegularExpressions.Regex.IsMatch(f.TrimStart('#'), @"^v\d+\.");

    public static SnapshotResult Read(string fragment)
    {
        var text = fragment.TrimStart('#');
        if (!text.StartsWith(Prefix, StringComparison.Ordinal))
            return Problem(IsSnapshot(text)
                ? "This link was made by a newer version of Motif Sketch, so this version can't open it."
                : "This link isn't a Motif Sketch snapshot.");

        string json;
        try
        {
            var data = text[Prefix.Length..].Replace('-', '+').Replace('_', '/');
            data = data.PadRight(data.Length + (4 - data.Length % 4) % 4, '=');
            using var input = new MemoryStream(Convert.FromBase64String(data));
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(deflate, Encoding.UTF8);
            json = reader.ReadToEnd();
        }
        catch (Exception e) when (e is FormatException or InvalidDataException or IOException)
        {
            return Problem("This snapshot link is damaged or incomplete. Ask for the link again, and copy all of it.");
        }

        var loaded = ProjectJson.Load(json);
        return loaded.Project is { } project
            ? new SnapshotResult(project, null)
            : Problem("This snapshot link holds a project that can't be opened: " + string.Join(" ", loaded.Problems));
    }

    private static SnapshotResult Problem(string problem) => new(null, problem);
}
