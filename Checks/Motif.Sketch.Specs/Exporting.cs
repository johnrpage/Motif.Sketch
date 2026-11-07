using System.Globalization;
using System.Xml.Linq;

namespace Motif.Sketch.Specs;

public sealed class Exporting
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    private static XDocument Parse(string svg) => XDocument.Parse(svg);

    [Fact]
    public async Task The_context_diagram_exports_as_well_formed_svg_with_every_element_and_relationship()
    {
        var h = await Harness.WithExample();

        (await h.Tasks.ExportContextDiagram()).Succeeded.Should().BeTrue();

        var (name, svg) = h.DiagramExports.Written.Should().ContainSingle().Which;
        name.Should().Be("Motif Sketch - Context Diagram.svg");
        var doc = Parse(svg);
        doc.Root!.Name.Should().Be(Svg + "svg");
        doc.Descendants(Svg + "g").Where(g => (string?)g.Attribute("class") == "element").Select(g => (string?)g.Attribute("data-id"))
            .Should().BeEquivalentTo(h.Queries.Context.Elements.Select(e => e.Id));
        doc.Descendants(Svg + "g").Count(g => (string?)g.Attribute("class") == "relationship").Should().Be(7);
        doc.Descendants(Svg + "text").Select(t => t.Value).Should().Contain(["Architect", "Browser Storage"]);
    }

    [Fact]
    public async Task Exports_use_the_light_palette_and_leave_out_screen_only_parts()
    {
        var h = await Harness.WithExample();
        h.Tasks.DeleteElement(h.Element("Clipboard").Id);
        await h.Tasks.ExportContextDiagram();
        await h.Tasks.ExportSketch();

        foreach (var (_, svg) in h.DiagramExports.Written)
        {
            svg.Should().NotContain("$").And.NotContain("var(--").And.NotContain("screen-only").And.NotContain("grip");
            svg.Should().Contain("IBM Plex Sans");
            Parse(svg).Descendants().Attributes("fill").Select(a => a.Value)
                .Should().OnlyContain(v => v == "none" || v.StartsWith('#'));
        }
        // The sketch's centre divider, and the "source deleted" flag, are on screen only.
        Parse(h.DiagramExports.Written[1].Svg).Descendants(Svg + "line").Should()
            .NotContain(l => (string?)l.Attribute("stroke-dasharray") == "6 6");
    }

    [Fact]
    public async Task The_sketch_export_shows_every_port_and_adapter_and_checks_are_pale_slate()
    {
        var h = await Harness.WithExample();

        await h.Tasks.ExportSketch();

        var doc = Parse(h.DiagramExports.Written.Single().Svg);
        doc.Descendants(Svg + "g").Where(g => (string?)g.Attribute("class") == "port").Should().HaveCount(6);
        var adapters = doc.Descendants(Svg + "g").Where(g => (string?)g.Attribute("class") == "adapter").ToList();
        adapters.Should().HaveCount(12);
        var specs = adapters.Single(a => (string?)a.Attribute("data-kind") == "checks").Element(Svg + "rect")!;
        ((string?)specs.Attribute("fill")).Should().Be("#E2E8F0");
        specs.Attribute("stroke-dasharray").Should().BeNull();
    }

    [Fact]
    public async Task The_screen_and_the_export_draw_the_same_shapes_in_the_same_places()
    {
        var h = await Harness.WithExample();
        await h.Tasks.ExportContextDiagram();
        var doc = Parse(h.DiagramExports.Written.Single().Svg);

        foreach (var element in h.Queries.Context.Elements.Where(e => e.Type != ElementType.Storage))
        {
            var rect = doc.Descendants(Svg + "g").Single(g => (string?)g.Attribute("data-id") == element.Id).Element(Svg + "rect")!;
            double At(string a) => double.Parse((string)rect.Attribute(a)!, CultureInfo.InvariantCulture);
            new Rect(At("x"), At("y"), At("width"), At("height")).Should().Be(element.Box);
        }
        var sceneTexts = h.Queries.ContextScene.All().Where(s => s.Text is not null).Select(s => s.Text);
        doc.Descendants(Svg + "text").Select(t => t.Value).Should().Equal(sceneTexts);
    }

    [Fact]
    public async Task Exports_warn_but_still_go_ahead_when_items_are_unplaced()
    {
        var h = await Harness.WithExample();
        var id = h.Tasks.AddElement(ElementType.ExternalSystem);
        var name = h.Queries.Context.Element(id)!.Name;

        var outcome = await h.Tasks.ExportSketch();

        outcome.Succeeded.Should().BeTrue();
        outcome.Warnings.Should().ContainSingle().Which.Should().Contain(name);
        h.Queries.CheckExport().Unplaced.Should().Equal(name);
    }

    [Fact]
    public async Task The_plan_lists_unplaced_items_and_warns_about_them()
    {
        var h = await Harness.WithExample();
        h.Tasks.RemoveSource(h.Adapter("Web App").Id, h.Element("Viewer").Id);

        var outcome = await h.Tasks.ExportPlan();

        outcome.Succeeded.Should().BeTrue();
        outcome.Warnings.Should().ContainSingle();
        h.SolutionStubs.Written.Single().Markdown.Should().Contain("- Viewer (primary actor, Web browser (snapshot link))");
    }

    [Theory]
    [InlineData("Domain")]
    [InlineData("Fakes")]
    [InlineData("Sketch")]
    public async Task A_reserved_port_name_blocks_the_plan(string name)
    {
        var h = await Harness.WithExample();
        h.Tasks.RenamePort(h.Port("Clipboard").Id, name);

        var outcome = await h.Tasks.ExportPlan();

        outcome.Succeeded.Should().BeFalse();
        outcome.Problems.Should().Contain(p => p.Contains("can't be called"));
        h.SolutionStubs.Written.Should().BeEmpty();
    }

    [Fact]
    public async Task Clashing_class_names_block_the_plan()
    {
        var h = await Harness.WithExample();
        h.Tasks.RenamePort(h.Port("Clipboard").Id, "Autosave");

        var outcome = await h.Tasks.ExportPlan();

        outcome.Problems.Should().Contain(p => p.Contains("interface called 'Autosave'"));
    }

    [Fact]
    public async Task An_unusable_system_name_blocks_the_plan()
    {
        var h = await Harness.WithExample();
        h.Tasks.RenameElement(h.System.Id, "42");

        (await h.Tasks.ExportPlan()).Problems.Should().Contain(p => p.Contains("usable solution name"));
    }

    [Fact]
    public void Code_names_keep_dots_in_the_root_and_two_letter_acronyms_in_capitals()
    {
        CodeName.Root("Motif.Sketch").Should().Be("Motif.Sketch");
        CodeName.Root("Motif Sketch").Should().Be("MotifSketch");
        CodeName.From("IndexedDB").Should().Be("IndexedDB");
        CodeName.From("WPF UI").Should().Be("WpfUI");
        CodeName.From("SVG Files").Should().Be("SvgFiles");
        SolutionPlanner.ClassFor("Clipboard", "Browser", null, AdapterKind.Real).Should().Be("BrowserClipboard");
        SolutionPlanner.ClassFor("Projects", "FakeProjects", null, AdapterKind.Fake).Should().Be("FakeProjects");
    }

    [Fact]
    public async Task Each_port_gets_a_fake_or_checks_even_if_none_is_drawn()
    {
        var h = await Harness.WithExample();
        h.Tasks.RemoveAdapter(h.Adapter("Fake Clipboard").Id);
        h.Tasks.RemoveAdapter(h.Adapter("Specs").Id);

        await h.Tasks.ExportPlan();

        var md = h.SolutionStubs.Written.Single().Markdown;
        md.Should().Contain("| Clipboard | Fake Clipboard | Fake | - | `Motif.Sketch.Fakes` (`Clipboard/`) | `FakeClipboard` | Added by the plan");
        md.Should().Contain("| User | Specs | Checks | xUnit | `Motif.Sketch.Specs` | - | Added by the plan");
    }

    /// <summary>The golden file: the plan Motif Sketch exports from its own design.</summary>
    [Fact]
    public async Task The_plan_exported_from_the_example_matches_the_golden_file()
    {
        var h = await Harness.WithExample();

        (await h.Tasks.ExportPlan()).Succeeded.Should().BeTrue();

        var (name, markdown) = h.SolutionStubs.Written.Single();
        name.Should().Be("Motif Sketch - plan.md");
        var golden = Path.Combine(AppContext.BaseDirectory, "Golden", "Motif.Sketch.plan.md");
        if (Environment.GetEnvironmentVariable("UPDATE_GOLDEN") == "1")
            File.WriteAllText(Path.Combine(SourceDirectory(), "Golden", "Motif.Sketch.plan.md"), markdown);
        markdown.Should().Be(File.ReadAllText(golden).ReplaceLineEndings("\n"));
    }

    private static string SourceDirectory([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
        Path.GetDirectoryName(path)!;
}
