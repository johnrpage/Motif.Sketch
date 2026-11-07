namespace Motif.Sketch.Specs;

public sealed class SketchingAContextDiagram
{
    [Fact]
    public async Task A_new_project_has_exactly_one_system_and_nothing_else()
    {
        var h = await Harness.Started();

        h.Queries.Context.Elements.Should().ContainSingle().Which.Type.Should().Be(ElementType.System);
        h.Queries.Context.Relationships.Should().BeEmpty();
        h.Queries.HasSketch.Should().BeFalse();
    }

    [Theory]
    [InlineData(ElementType.PrimaryActor, "Primary Actor 1")]
    [InlineData(ElementType.SecondaryActor, "Secondary Actor 1")]
    [InlineData(ElementType.ExternalSystem, "External System 1")]
    [InlineData(ElementType.Storage, "Storage 1")]
    public async Task Adding_an_element_gives_it_a_default_name_and_its_one_relationship(ElementType type, string name)
    {
        var h = await Harness.Started();

        var id = h.Tasks.AddElement(type);

        h.Queries.Context.Element(id)!.Name.Should().Be(name);
        var relationship = h.Queries.Context.RelationshipOf(id)!;
        new[] { relationship.From, relationship.To }.Should().BeEquivalentTo([id, h.System.Id]);
    }

    [Fact]
    public async Task Primary_actors_point_at_the_system_and_the_system_points_at_everything_else()
    {
        var h = await Harness.Started();

        var actor = h.Tasks.AddElement(ElementType.PrimaryActor);
        var storage = h.Tasks.AddElement(ElementType.Storage);

        h.Queries.Context.RelationshipOf(actor)!.From.Should().Be(actor);
        h.Queries.Context.RelationshipOf(storage)!.From.Should().Be(h.System.Id);
    }

    [Fact]
    public async Task Default_names_count_up_per_type()
    {
        var h = await Harness.Started();

        h.Tasks.AddElement(ElementType.Storage);
        h.Tasks.AddElement(ElementType.Storage);

        h.Queries.Context.Elements.Select(e => e.Name).Should().Contain(["Storage 1", "Storage 2"]);
    }

    [Fact]
    public async Task The_system_cant_be_deleted_or_be_added_twice()
    {
        var h = await Harness.Started();

        h.Invoking(x => x.Tasks.DeleteElement(x.System.Id)).Should().Throw<ProjectRuleException>();
        h.Invoking(x => x.Tasks.AddElement(ElementType.System)).Should().Throw<ProjectRuleException>();
        h.Queries.Context.Elements.Should().ContainSingle();
    }

    [Fact]
    public async Task Deleting_an_element_deletes_its_relationship()
    {
        var h = await Harness.Started();
        var id = h.Tasks.AddElement(ElementType.ExternalSystem);

        h.Tasks.DeleteElement(id);

        h.Queries.Context.Elements.Should().ContainSingle();
        h.Queries.Context.Relationships.Should().BeEmpty();
    }

    [Fact]
    public async Task Names_descriptions_technology_labels_and_directions_can_be_edited()
    {
        var h = await Harness.Started();
        var id = h.Tasks.AddElement(ElementType.PrimaryActor);
        var relationship = h.Queries.Context.RelationshipOf(id)!.Id;

        h.Tasks.RenameElement(id, "  Customer ");
        h.Tasks.DescribeElement(id, "Buys things");
        h.Tasks.SetElementTechnology(id, "Web browser");
        h.Tasks.SetRelationshipLabel(relationship, "Places orders");
        h.Tasks.SetRelationshipDirection(relationship, RelationshipDirection.TwoWay);

        var element = h.Queries.Context.Element(id)!;
        element.Should().BeEquivalentTo(new { Name = "Customer", Description = "Buys things", Technology = "Web browser" });
        h.Queries.Context.RelationshipOf(id)!.Should().BeEquivalentTo(new { Label = "Places orders", Direction = RelationshipDirection.TwoWay });
    }

    [Fact]
    public async Task An_element_needs_a_name()
    {
        var h = await Harness.Started();
        var id = h.Tasks.AddElement(ElementType.Storage);

        h.Invoking(x => x.Tasks.RenameElement(id, "  ")).Should().Throw<ProjectRuleException>();
        h.Queries.Context.Element(id)!.Name.Should().Be("Storage 1");
    }

    [Fact]
    public async Task Dragging_an_element_pins_it()
    {
        var h = await Harness.Started();
        var id = h.Tasks.AddElement(ElementType.Storage);

        h.Tasks.MoveElement(id, 600, 400);

        h.Queries.Context.Element(id)!.Should().BeEquivalentTo(new { Pinned = true, Box = new Rect(515, 355, 170, 90) });
    }

    [Fact]
    public async Task Auto_layout_all_releases_every_pin()
    {
        var h = await Harness.Started();
        var id = h.Tasks.AddElement(ElementType.Storage);
        var placed = h.Queries.Context.Element(id)!.Box;
        h.Tasks.MoveElement(id, 900, 900);

        h.Tasks.AutoLayout();
        h.Queries.Context.Element(id)!.Pinned.Should().BeTrue();

        h.Tasks.AutoLayoutAll();
        h.Queries.Context.Element(id)!.Should().BeEquivalentTo(new { Pinned = false, Box = placed });
    }

    [Fact]
    public async Task The_project_title_is_renamed_on_its_own()
    {
        var h = await Harness.Started();

        h.Tasks.RenameProject("Online Shop");

        h.Queries.Title.Should().Be("Online Shop");
    }
}
