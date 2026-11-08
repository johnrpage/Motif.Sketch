using System.Net;

namespace Motif.Sketch.Adapters;

/// <summary>
/// Reads a public repo's sketch from raw.githubusercontent.com, at HEAD (the default branch), as Motif Glossary does.
/// No sign-in and no API rate limit; raw.githubusercontent.com allows cross-origin reads, so this works from Pages.
/// </summary>
public sealed class GitHubRepos(HttpClient http) : Repos
{
    public const string Host = "https://raw.githubusercontent.com";

    public static Uri Url(RepoName repo, string path) =>
        new($"{Host}/{Uri.EscapeDataString(repo.Owner)}/{Uri.EscapeDataString(repo.Name)}/HEAD/{path}");

    public async Task<string?> ReadSketch(RepoName repo, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync(Url(repo, RepoLink.SketchPath), cancellationToken);
        // GitHub says 404 both for a missing file and for a repo that isn't public.
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
