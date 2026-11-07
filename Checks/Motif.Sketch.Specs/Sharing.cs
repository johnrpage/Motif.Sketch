namespace Motif.Sketch.Specs;

public sealed class Sharing
{
    private const string Page = "https://example.github.io/Motif.Sketch/";

    [Fact]
    public async Task Copy_link_puts_a_snapshot_of_the_whole_project_in_the_fragment()
    {
        var h = await Harness.WithExample();

        var outcome = await h.Tasks.CopyLink(Page + "#old");

        outcome.Succeeded.Should().BeTrue();
        h.Clipboard.Text!.Should().StartWith(Page + "#v1.").And.Be(outcome.Link);
        outcome.IsLong.Should().BeFalse();
    }

    [Fact]
    public async Task Opening_the_link_gives_the_same_project_read_only()
    {
        var h = await Harness.WithExample();
        await h.Tasks.CopyLink(Page);
        var text = h.Clipboard.Text!;
        var fragment = text[text.IndexOf('#')..];

        var viewer = new Harness();
        var started = await viewer.Tasks.Start(fragment);

        started.StartedAs.Should().Be(StartedAs.Snapshot);
        viewer.Queries.ProjectJsonText.Should().Be(h.Queries.ProjectJsonText);
        viewer.Queries.Should().BeEquivalentTo(new { Mode = AppMode.Viewer, IsReadOnly = true, HasSketch = true });
        viewer.Invoking(x => x.Tasks.AddElement(ElementType.Storage)).Should().Throw<ProjectRuleException>();
    }

    [Fact]
    public async Task Viewer_mode_never_touches_autosave()
    {
        var mine = new AutosavedWork("{ my own work }", "mine.motifsketch.json", null, true);
        var viewer = new Harness();
        viewer.Autosave.Work = mine;

        await viewer.Tasks.Start("#" + SnapshotLink.Fragment(Example.Project()));
        await viewer.Tasks.ExportContextDiagram();
        await viewer.Tasks.ExportSketch();
        await viewer.Tasks.ExportPlan();

        viewer.Autosave.Stores.Should().Be(0);
        viewer.Autosave.Work.Should().Be(mine);
        viewer.DiagramExports.Written.Should().HaveCount(2);
        viewer.SolutionStubs.Written.Should().ContainSingle();
    }

    [Fact]
    public async Task Make_a_copy_to_edit_turns_the_snapshot_into_ordinary_unsaved_work()
    {
        var viewer = new Harness();
        await viewer.Tasks.Start("#" + SnapshotLink.Fragment(Example.Project()));

        await viewer.Tasks.MakeCopyToEdit();

        viewer.Queries.Should().BeEquivalentTo(new { Mode = AppMode.Architect, HasUnsavedChanges = true, FileName = (string?)null });
        viewer.Autosave.Work!.Json.Should().Be(viewer.Queries.ProjectJsonText);
        viewer.Tasks.AddElement(ElementType.Storage);
    }

    [Theory]
    [InlineData("#v1.not-really-deflate", "damaged")]
    [InlineData("#v9.abc", "newer version")]
    public async Task A_bad_link_gives_a_friendly_error_and_opens_the_visitors_own_work(string fragment, string problem)
    {
        var h = new Harness();
        h.Autosave.Work = new AutosavedWork(Example.Json(), null, null, false);

        var started = await h.Tasks.Start(fragment);

        started.Problem.Should().Contain(problem);
        started.StartedAs.Should().Be(StartedAs.Restored);
        h.Queries.Mode.Should().Be(AppMode.Architect);
    }

    [Fact]
    public void A_snapshot_holding_an_invalid_project_is_refused()
    {
        var bad = Project.CreateNew();
        bad.ContextDiagram.Elements.Clear();

        SnapshotLink.Read(SnapshotLink.Fragment(bad)).Problem.Should().Contain("can't be opened");
    }

    [Fact]
    public async Task A_very_long_link_is_still_copied_but_flagged()
    {
        var h = await Harness.Started();
        for (var i = 0; i < 160; i++)
        {
            var id = h.Tasks.AddElement(ElementType.Storage);
            h.Tasks.DescribeElement(id, Guid.NewGuid().ToString() + Guid.NewGuid() + Guid.NewGuid());
        }

        var outcome = await h.Tasks.CopyLink(Page);

        outcome.Should().BeEquivalentTo(new { Succeeded = true, IsLong = true });
        h.Clipboard.Text.Should().Be(outcome.Link);
    }
}
