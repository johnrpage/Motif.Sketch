namespace Motif.Sketch.Specs;

public sealed class UndoAndRedo
{
    [Fact]
    public async Task Every_edit_in_either_view_is_one_step_in_one_history()
    {
        var h = await Harness.Started();
        h.Tasks.AddElement(ElementType.PrimaryActor);
        h.Tasks.StartSketch();
        h.Tasks.AddPort(Side.Driven);

        h.Tasks.Undo();
        h.Queries.Sketch!.Ports.Should().ContainSingle();
        h.Tasks.Undo();
        h.Queries.HasSketch.Should().BeFalse();
        h.Tasks.Undo();
        h.Queries.Context.Elements.Should().ContainSingle();
        h.Queries.CanUndo.Should().BeFalse();

        h.Tasks.Redo();
        h.Tasks.Redo();
        h.Queries.HasSketch.Should().BeTrue();
        h.Queries.CanRedo.Should().BeTrue();
    }

    [Fact]
    public async Task A_new_edit_clears_redo()
    {
        var h = await Harness.Started();
        h.Tasks.AddElement(ElementType.Storage);
        h.Tasks.Undo();

        h.Tasks.AddElement(ElementType.ExternalSystem);

        h.Queries.CanRedo.Should().BeFalse();
    }

    [Fact]
    public async Task An_edit_that_breaks_a_rule_changes_nothing_and_adds_no_step()
    {
        var h = await Harness.Started();
        var before = h.Queries.ProjectJsonText;

        h.Invoking(x => x.Tasks.DeleteElement(x.System.Id)).Should().Throw<ProjectRuleException>();

        h.Queries.ProjectJsonText.Should().Be(before);
        h.Queries.CanUndo.Should().BeFalse();
    }

    [Fact]
    public async Task Undoing_back_to_the_saved_state_clears_the_unsaved_marker()
    {
        var h = await Harness.Started();
        await h.Tasks.SaveAs();
        h.Tasks.AddElement(ElementType.Storage);
        h.Queries.HasUnsavedChanges.Should().BeTrue();

        h.Tasks.Undo();

        h.Queries.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task The_history_is_capped()
    {
        var h = await Harness.Started();
        for (var i = 0; i < MotifSketchApplication.HistoryLimit + 20; i++)
            h.Tasks.RenameProject($"Title {i}");

        var steps = 0;
        while (h.Queries.CanUndo) { h.Tasks.Undo(); steps++; }

        steps.Should().Be(MotifSketchApplication.HistoryLimit);
    }
}
