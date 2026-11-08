namespace Motif.Sketch.Specs;

public sealed class TheExample
{
    [Fact]
    public async Task Open_example_loads_Motif_Sketchs_own_design_as_unsaved_work()
    {
        var h = await Harness.WithExample();

        h.Queries.Title.Should().Be("Motif Sketch");
        h.Queries.Should().BeEquivalentTo(new { HasUnsavedChanges = true, FileName = (string?)null, HasSketch = true });
        h.Queries.Sketch!.Ports.Select(p => p.Name).Should().Equal(
            "User", "Projects", "Autosave", "Diagram Exports", "Solution Stubs", "Clipboard", "Repos", "Glossaries");
        h.Adapter("Web App").Sources.Select(s => s.Name).Should().Equal("Architect", "Viewer");
        h.Adapter("Specs").Kind.Should().Be(AdapterKind.Checks);
        h.Queries.Unplaced.Should().BeEmpty();
    }

    [Fact]
    public void The_bundled_file_is_already_in_the_saved_format()
    {
        ProjectJson.Save(Example.Project()).Should().Be(Example.Json().ReplaceLineEndings("\n"));
    }

    [Fact]
    public void The_bundled_file_is_laid_out_as_the_layout_would_place_it()
    {
        var project = Example.Project();

        ContextLayout.Apply(project.ContextDiagram).Should().BeFalse();
    }
}
