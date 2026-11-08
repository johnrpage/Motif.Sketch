using System.Text.RegularExpressions;

namespace Motif.Sketch;

/// <summary>A public GitHub repo, owner/name.</summary>
/// <remarks>The same rules as Motif Glossary's, so a <c>#repo=</c> link one tool accepts, the other does too.</remarks>
public sealed partial record RepoName(string Owner, string Name)
{
    public override string ToString() => $"{Owner}/{Name}";

    /// <summary>Reads "owner/name", or a github.com URL to the repo or anything in it.</summary>
    public static bool TryParse(string? text, out RepoName repo)
    {
        repo = null!;
        var s = text?.Trim() ?? "";
        var url = false;
        foreach (var prefix in new[] { "https://", "http://", "www.", "github.com/" })
        {
            if (!s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            s = s[prefix.Length..];
            url = true;
        }
        var parts = s.Split(['/', '?', '#'], StringSplitOptions.RemoveEmptyEntries);
        // A plain name is exactly owner/name; a URL can go on into the repo.
        if (parts.Length < 2 || (!url && parts.Length > 2)) return false;
        var name = parts[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? parts[1][..^4] : parts[1];
        if (!OwnerPattern().IsMatch(parts[0]) || !NamePattern().IsMatch(name) || name is "." or "..") return false;
        repo = new RepoName(parts[0], name);
        return true;
    }

    [GeneratedRegex(@"^[A-Za-z0-9](?:[A-Za-z0-9-]{0,38})$")]
    private static partial Regex OwnerPattern();

    [GeneratedRegex(@"^[A-Za-z0-9._-]{1,100}$")]
    private static partial Regex NamePattern();
}

/// <summary>
/// A link ending <c>#repo=owner/name</c>: every Motif tool opens that repo's <c>motif/</c> folder from it,
/// and the tool switcher carries it from one tool to the next. Here it opens the repo's sketch read-only.
/// </summary>
public static class RepoLink
{
    public const string Prefix = "repo=";

    /// <summary>Where a repo's sketch lives.</summary>
    public const string SketchPath = "motif/sketch.motifsketch.json";

    public static string Fragment(RepoName repo) => Prefix + repo;

    public static bool TryRead(string? fragment, out RepoName repo)
    {
        repo = null!;
        var f = Uri.UnescapeDataString(fragment?.TrimStart('#') ?? "");
        return f.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            && RepoName.TryParse(f[Prefix.Length..], out repo);
    }
}
