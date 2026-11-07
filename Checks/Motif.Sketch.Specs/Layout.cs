namespace Motif.Sketch.Specs;

public sealed class Layout
{
    private static async Task<Harness> Busy()
    {
        var h = await Harness.Started();
        foreach (var type in new[] { ElementType.PrimaryActor, ElementType.PrimaryActor, ElementType.PrimaryActor })
            h.Tasks.AddElement(type);
        for (var i = 0; i < 7; i++) h.Tasks.AddElement(ElementType.Storage);
        h.Tasks.AddElement(ElementType.ExternalSystem);
        h.Tasks.AddElement(ElementType.SecondaryActor);
        h.Tasks.AddElement(ElementType.SecondaryActor);
        return h;
    }

    [Fact]
    public async Task Context_layout_puts_primary_actors_left_and_everything_else_right_top_to_bottom()
    {
        var h = await Busy();
        var elements = h.Queries.Context.Elements;
        var system = h.System.Box;

        elements.Where(e => e.Type == ElementType.PrimaryActor).Should().OnlyContain(e => e.Box.Right < system.X);
        elements.Where(e => e.Type is not ElementType.PrimaryActor and not ElementType.System)
            .Should().OnlyContain(e => e.Box.X > system.Right);

        double TopOf(ElementType t) => elements.Where(e => e.Type == t).Min(e => e.Box.Y);
        double BottomOf(ElementType t) => elements.Where(e => e.Type == t).Max(e => e.Box.Bottom);
        BottomOf(ElementType.SecondaryActor).Should().BeLessThan(TopOf(ElementType.ExternalSystem));
        BottomOf(ElementType.ExternalSystem).Should().BeLessThan(TopOf(ElementType.Storage));
    }

    [Fact]
    public async Task Context_layout_never_overlaps_and_wraps_long_groups_into_a_second_column()
    {
        var h = await Busy();
        var boxes = h.Queries.Context.Elements.Select(e => e.Box).ToList();

        for (var i = 0; i < boxes.Count; i++)
            for (var j = i + 1; j < boxes.Count; j++)
                boxes[i].Intersects(boxes[j]).Should().BeFalse();
        h.Queries.Context.Elements.Where(e => e.Type == ElementType.Storage).Select(e => e.Box.X).Distinct().Should().HaveCount(2);
    }

    [Fact]
    public async Task Context_layout_is_deterministic()
    {
        var a = await Busy();
        var b = await Busy();

        a.Queries.Context.Elements.Select(e => (e.Type, e.Name, e.Box))
            .Should().Equal(b.Queries.Context.Elements.Select(e => (e.Type, e.Name, e.Box)));
    }

    [Fact]
    public async Task Pinned_elements_stay_put_and_nothing_is_placed_on_top_of_them()
    {
        var h = await Harness.Started();
        var first = h.Tasks.AddElement(ElementType.Storage);
        var spot = h.Queries.Context.Element(first)!.Box.Centre;
        h.Tasks.MoveElement(first, spot.X, spot.Y);

        var second = h.Tasks.AddElement(ElementType.Storage);
        h.Tasks.AutoLayout();

        h.Queries.Context.Element(first)!.Box.Centre.Should().Be(spot);
        h.Queries.Context.Element(second)!.Box.Intersects(h.Queries.Context.Element(first)!.Box).Should().BeFalse();
    }

    [Fact]
    public async Task A_long_label_widens_the_gap_to_the_system_so_it_fits()
    {
        var h = await Harness.Started();
        var id = h.Tasks.AddElement(ElementType.PrimaryActor);
        var before = h.Queries.Context.Element(id)!.Box.X;

        h.Tasks.SetRelationshipLabel(h.Queries.Context.RelationshipOf(id)!.Id,
            "Places, tracks, amends and sometimes cancels a great many orders every single day of the week");

        h.Queries.Context.Element(id)!.Box.X.Should().BeLessThan(before);
    }

    [Fact]
    public async Task Long_names_wrap_onto_two_lines_and_are_cut_after_that()
    {
        var h = await Harness.Started();
        var id = h.Tasks.AddElement(ElementType.ExternalSystem);

        h.Tasks.RenameElement(id, "A remarkably long external system name that goes on and on and on");

        var lines = h.Queries.Context.Element(id)!.NameLines;
        lines.Should().HaveCount(2);
        lines[^1].Should().EndWith("…");
        lines.Should().OnlyContain(l => TextWrap.Width(l, DiagramStyle.NameFontSize, true) <= 150);
    }

    [Fact]
    public async Task Sketch_layout_puts_the_User_port_at_nine_oclock_and_driven_ports_on_the_right()
    {
        var h = await Harness.WithExample();
        var view = h.Queries.Sketch!;

        var user = h.Port("User");
        user.Centre.Y.Should().BeApproximately(0, 0.01);
        user.Centre.X.Should().BeLessThan(0);
        view.Ports.Where(p => p.Side == Side.Driven).Should().OnlyContain(p => p.Centre.X > 0);
        view.Ports.Where(p => p.Side == Side.Driven).Select(p => p.Centre.Y).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Sketch_layout_never_overlaps_adapters_and_is_deterministic()
    {
        var a = await Harness.WithExample();
        var b = await Harness.WithExample();
        for (var i = 0; i < 4; i++) a.Tasks.AddAdapter(a.Port("Projects").Id, AdapterKind.Simulated);
        for (var i = 0; i < 4; i++) b.Tasks.AddAdapter(b.Port("Projects").Id, AdapterKind.Simulated);

        var boxes = a.Adapters.Select(x => x.Box).ToList();
        for (var i = 0; i < boxes.Count; i++)
            for (var j = i + 1; j < boxes.Count; j++)
                boxes[i].Intersects(boxes[j]).Should().BeFalse();
        a.Adapters.Select(x => x.Box).Should().Equal(b.Adapters.Select(x => x.Box));
    }
}
