namespace Motif.Sketch;

/// <summary>Driven port: reads a public GitHub repo's sketch from its default branch.</summary>
public interface Repos
{
    /// <summary>
    /// The text of the repo's <c>motif/sketch.motifsketch.json</c>, or null if it has none (or isn't public:
    /// GitHub says the same for both). Throws <see cref="HttpRequestException"/> when GitHub can't be reached.
    /// </summary>
    Task<string?> ReadSketch(RepoName repo, CancellationToken cancellationToken = default);
}
