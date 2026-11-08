namespace Motif.Sketch.Fakes;

/// <summary>Public repos held in memory. A repo not added has no sketch; <see cref="Unreachable"/> makes every read fail.</summary>
public sealed class FakeRepos : Repos
{
    private readonly Dictionary<string, string> sketches = new(StringComparer.OrdinalIgnoreCase);

    public bool Unreachable { get; set; }

    /// <summary>The repos read, in order.</summary>
    public List<RepoName> Reads { get; } = [];

    public FakeRepos Add(string repo, string sketchFile)
    {
        sketches[repo] = sketchFile;
        return this;
    }

    public Task<string?> ReadSketch(RepoName repo, CancellationToken cancellationToken = default)
    {
        Reads.Add(repo);
        if (Unreachable) throw new HttpRequestException("no network");
        return Task.FromResult(sketches.GetValueOrDefault(repo.ToString()));
    }
}
