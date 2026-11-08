namespace Motif.Sketch.Specs;

/// <summary>A <c>#repo=owner/name</c> link: the project every Motif tool opens, here the repo's sketch.</summary>
public sealed class OpeningARepo
{
    private static readonly AutosavedWork RealWork = new(Example.Json(), "mine.motifsketch.json", null, true);

    private static async Task<(Harness Harness, StartResult Started)> Start(string fragment, Action<Harness>? arrange = null)
    {
        var h = new Harness();
        h.Repos.Add("motif-dotnet/Motif.Sketch", Example.Json());
        arrange?.Invoke(h);
        var started = await h.Tasks.Start(fragment);
        return (h, started);
    }

    [Theory]
    [InlineData("#repo=motif-dotnet/Motif.Sketch")]
    [InlineData("#repo=https://github.com/motif-dotnet/Motif.Sketch")]
    [InlineData("#repo%3Dmotif-dotnet%2FMotif.Sketch")]
    public async Task A_repo_link_opens_the_repos_sketch_read_only(string fragment)
    {
        var (h, started) = await Start(fragment);

        started.Should().Be(new StartResult(StartedAs.Repo));
        h.Repos.Reads.Should().Equal(new RepoName("motif-dotnet", "Motif.Sketch"));
        h.Queries.Should().BeEquivalentTo(new { Mode = AppMode.Viewer, IsReadOnly = true, HasSketch = true, Title = "Motif Sketch" });
        h.Queries.Repo.Should().Be(new RepoName("motif-dotnet", "Motif.Sketch"));
        h.Invoking(x => x.Tasks.AddElement(ElementType.Storage)).Should().Throw<ProjectRuleException>();
    }

    [Fact]
    public async Task A_repo_link_never_touches_autosave()
    {
        var (h, _) = await Start("#repo=motif-dotnet/Motif.Sketch", h => h.Autosave.Work = RealWork);

        await h.Tasks.ExportPlan();

        h.Autosave.Stores.Should().Be(0);
        h.Autosave.Work.Should().Be(RealWork);
    }

    [Fact]
    public async Task Make_a_copy_turns_the_repos_sketch_into_ordinary_work_with_no_repo()
    {
        var (h, _) = await Start("#repo=motif-dotnet/Motif.Sketch");

        await h.Tasks.MakeCopyToEdit();

        h.Queries.Should().BeEquivalentTo(new { Mode = AppMode.Architect, HasUnsavedChanges = true });
        h.Queries.Repo.Should().BeNull();
        h.Autosave.Work!.Json.Should().Be(h.Queries.ProjectJsonText);
    }

    [Fact]
    public async Task A_repo_with_no_sketch_says_so_and_opens_your_own_work()
    {
        var (h, started) = await Start("#repo=someone/no-sketch", h => h.Autosave.Work = RealWork);

        started.StartedAs.Should().Be(StartedAs.Restored);
        started.Problem.Should().Contain("someone/no-sketch").And.Contain("motif/sketch.motifsketch.json");
        h.Queries.Mode.Should().Be(AppMode.Architect);
        h.Queries.Repo.Should().BeNull();
    }

    [Fact]
    public async Task A_repo_GitHub_cant_be_reached_for_says_so()
    {
        var (_, started) = await Start("#repo=motif-dotnet/Motif.Sketch", h => h.Repos.Unreachable = true);

        started.StartedAs.Should().Be(StartedAs.Fresh);
        started.Problem.Should().Contain("GitHub couldn't be reached");
    }

    [Fact]
    public async Task A_repo_whose_sketch_isnt_a_sketch_says_so()
    {
        var (_, started) = await Start("#repo=someone/broken", h => h.Repos.Add("someone/broken", "{ not json"));

        started.StartedAs.Should().Be(StartedAs.Fresh);
        started.Problem.Should().Contain("isn't a sketch Motif Sketch can open");
    }

    [Theory]
    [InlineData("#repo=")]
    [InlineData("#repo=just-a-name")]
    [InlineData("#repo=a/b/c")]
    public async Task A_repo_link_without_a_proper_owner_and_name_is_ignored(string fragment)
    {
        var (h, started) = await Start(fragment);

        started.Should().Be(new StartResult(StartedAs.Fresh));
        h.Repos.Reads.Should().BeEmpty();
    }

    [Fact]
    public void A_repo_link_names_the_repo_the_same_way_Motif_Glossary_does() =>
        RepoLink.Fragment(new RepoName("motif-dotnet", "Motif.Sketch")).Should().Be("repo=motif-dotnet/Motif.Sketch");
}
