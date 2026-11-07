namespace Motif.Sketch.Specs;

public sealed class Autosaving
{
    [Fact]
    public async Task Every_edit_is_autosaved()
    {
        var h = await Harness.Started();

        h.Tasks.AddElement(ElementType.Storage);

        h.Autosave.Work!.Json.Should().Be(h.Queries.ProjectJsonText);
        h.Autosave.Work.Unsaved.Should().BeTrue();
    }

    [Fact]
    public async Task The_next_visit_restores_the_work_with_its_file_and_unsaved_marker()
    {
        var first = await Harness.Started();
        first.Projects.NextSaveAsName = "shop.motifsketch.json";
        await first.Tasks.SaveAs();
        first.Tasks.AddElement(ElementType.Storage);

        var next = new Harness();
        next.Autosave.Work = first.Autosave.Work;
        var started = await next.Tasks.Start(null);

        started.StartedAs.Should().Be(StartedAs.Restored);
        next.Queries.ProjectJsonText.Should().Be(first.Queries.ProjectJsonText);
        next.Queries.Should().BeEquivalentTo(new { FileName = "shop.motifsketch.json", HasUnsavedChanges = true });
    }

    [Fact]
    public async Task After_a_refresh_Save_still_goes_to_the_same_file()
    {
        var first = await Harness.Started();
        await first.Tasks.SaveAs();
        var handle = first.Projects.Writes[^1].Handle;

        var next = new Harness();
        next.Autosave.Work = first.Autosave.Work;
        await next.Tasks.Start(null);
        await next.Tasks.Save();

        next.Projects.Writes.Should().ContainSingle().Which.Handle.Should().Be(handle);
    }

    [Fact]
    public async Task With_nothing_autosaved_the_first_run_is_a_blank_project()
    {
        var h = new Harness();

        (await h.Tasks.Start(null)).StartedAs.Should().Be(StartedAs.Fresh);
        h.Queries.Context.Elements.Should().ContainSingle();
    }

    [Fact]
    public async Task A_second_tab_is_read_only_and_never_writes_autosave()
    {
        var h = new Harness();
        h.Autosave.OwnedElsewhere = true;
        h.Autosave.Work = new AutosavedWork(Example.Json(), null, null, false);

        (await h.Tasks.Start(null)).StartedAs.Should().Be(StartedAs.OtherTab);

        h.Queries.Should().BeEquivalentTo(new { Mode = AppMode.OtherTab, IsReadOnly = true });
        h.Queries.Title.Should().Be("Motif Sketch");
        h.Invoking(x => x.Tasks.AddElement(ElementType.Storage)).Should().Throw<ProjectRuleException>();
        h.Autosave.Stores.Should().Be(0);
    }

    [Fact]
    public async Task Use_this_tab_instead_hands_ownership_over_with_the_latest_work()
    {
        var h = new Harness();
        h.Autosave.OwnedElsewhere = true;
        await h.Tasks.Start(null);
        h.Autosave.Work = new AutosavedWork(Example.Json(), "latest.motifsketch.json", null, true);

        await h.Tasks.UseThisTab();

        h.Queries.Mode.Should().Be(AppMode.Architect);
        h.Queries.FileName.Should().Be("latest.motifsketch.json");
        h.Tasks.AddElement(ElementType.Storage);
        h.Autosave.Stores.Should().Be(1);
    }

    [Fact]
    public async Task The_first_tab_goes_read_only_when_another_takes_over()
    {
        var h = await Harness.Started();

        h.Autosave.TakenOverElsewhere();

        h.Queries.Mode.Should().Be(AppMode.OtherTab);
        h.Invoking(x => x.Tasks.AddElement(ElementType.Storage)).Should().Throw<ProjectRuleException>();
    }

    [Fact]
    public async Task Autosave_isnt_a_save_unsaved_still_means_not_saved_to_a_file()
    {
        var h = await Harness.Started();
        h.Tasks.AddElement(ElementType.Storage);

        h.Autosave.Stores.Should().BePositive();
        h.Queries.HasUnsavedChanges.Should().BeTrue();
    }
}
