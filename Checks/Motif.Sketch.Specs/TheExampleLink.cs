namespace Motif.Sketch.Specs;

public sealed class TheExampleLink
{
    private static readonly AutosavedWork RealWork = new(Example.Json(), "mine.motifsketch.json", null, true);

    [Theory]
    [InlineData("#example")]
    [InlineData("#Example")]
    public async Task An_example_link_opens_the_example_read_only(string fragment)
    {
        var h = await Harness.Started(fragment);

        h.Queries.Title.Should().Be("Motif Sketch");
        h.Queries.Should().BeEquivalentTo(new { Mode = AppMode.Viewer, IsReadOnly = true, HasSketch = true, IsEmpty = false });
        h.Invoking(x => x.Tasks.AddElement(ElementType.Storage)).Should().Throw<ProjectRuleException>();
    }

    [Fact]
    public async Task An_example_link_never_touches_autosave()
    {
        var h = new Harness();
        h.Autosave.Work = RealWork;

        var started = await h.Tasks.Start("#example");
        await h.Tasks.ExportPlan();

        started.StartedAs.Should().Be(StartedAs.Example);
        h.Autosave.Stores.Should().Be(0);
        h.Autosave.Work.Should().Be(RealWork);
    }

    [Fact]
    public async Task Make_a_copy_turns_the_example_into_ordinary_unsaved_work()
    {
        var h = await Harness.Started("#example");

        await h.Tasks.MakeCopyToEdit();

        h.Queries.Should().BeEquivalentTo(new { Mode = AppMode.Architect, HasUnsavedChanges = true });
        h.Autosave.Work!.Json.Should().Be(h.Queries.ProjectJsonText);
    }

    [Fact]
    public async Task A_phone_with_no_autosaved_work_gets_the_example()
    {
        var h = new Harness();

        var started = await h.Tasks.Start(null, exampleIfEmpty: true);

        started.StartedAs.Should().Be(StartedAs.Example);
        h.Queries.Should().BeEquivalentTo(new { Mode = AppMode.Viewer, Title = "Motif Sketch" });
    }

    [Fact]
    public async Task A_phone_whose_autosave_is_only_the_system_gets_the_example_and_keeps_that_autosave()
    {
        var empty = Project.CreateNew(systemName: "Named but nothing else");
        var work = new AutosavedWork(ProjectJson.Save(empty), null, null, true);
        var h = new Harness();
        h.Autosave.Work = work;

        var started = await h.Tasks.Start(null, exampleIfEmpty: true);

        started.StartedAs.Should().Be(StartedAs.Example);
        h.Autosave.Work.Should().Be(work);
    }

    [Fact]
    public async Task A_phone_with_real_autosaved_work_opens_that_work()
    {
        var h = new Harness();
        h.Autosave.Work = RealWork;

        var started = await h.Tasks.Start(null, exampleIfEmpty: true);

        started.StartedAs.Should().Be(StartedAs.Restored);
        h.Queries.Mode.Should().Be(AppMode.Architect);
    }

    [Fact]
    public async Task Without_the_phone_option_empty_work_still_starts_blank()
    {
        var h = await Harness.Started();

        h.Queries.Should().BeEquivalentTo(new { Mode = AppMode.Architect, IsEmpty = true });
    }

    [Fact]
    public async Task A_project_stops_being_empty_when_an_element_is_added_and_is_empty_again_after_New()
    {
        var h = await Harness.Started();

        h.Tasks.AddElement(ElementType.Storage);
        h.Queries.IsEmpty.Should().BeFalse();

        await h.Tasks.NewProject();
        h.Queries.IsEmpty.Should().BeTrue();
    }
}
