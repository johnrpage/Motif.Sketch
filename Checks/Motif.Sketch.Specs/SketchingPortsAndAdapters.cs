namespace Motif.Sketch.Specs;

public sealed class SketchingPortsAndAdapters
{
    /// <summary>A shop: two primary actors, a payment provider and a database.</summary>
    private static async Task<Harness> Shop()
    {
        var h = await Harness.Started();
        h.Tasks.RenameElement(h.System.Id, "Shop");
        h.Tasks.RenameElement(h.Tasks.AddElement(ElementType.PrimaryActor), "Customer");
        var staff = h.Tasks.AddElement(ElementType.PrimaryActor);
        h.Tasks.RenameElement(staff, "Staff");
        h.Tasks.SetElementTechnology(staff, "WPF");
        h.Tasks.RenameElement(h.Tasks.AddElement(ElementType.SecondaryActor), "Payments");
        h.Tasks.RenameElement(h.Tasks.AddElement(ElementType.Storage), "Orders DB");
        return h;
    }

    [Fact]
    public async Task Starting_the_sketch_makes_the_User_port_with_an_adapter_per_primary_actor_and_Specs()
    {
        var h = await Shop();

        h.Tasks.StartSketch();

        var user = h.Queries.Sketch!.Ports.Should().ContainSingle().Which;
        user.Should().BeEquivalentTo(new { Name = "User", IsUser = true, Side = Side.Driving });
        user.Adapters.Select(a => (a.Name, a.Kind, a.Technology)).Should().Equal(
            ("Customer", AdapterKind.Real, null), ("Staff", AdapterKind.Real, "WPF"), ("Specs", AdapterKind.Checks, "xUnit"));
        h.UnplacedNames.Should().Equal("Payments", "Orders DB");
    }

    [Fact]
    public async Task The_sketch_can_only_be_started_once()
    {
        var h = await Shop();
        h.Tasks.StartSketch();

        h.Invoking(x => x.Tasks.StartSketch()).Should().Throw<ProjectRuleException>();
    }

    [Fact]
    public async Task The_User_port_cant_be_deleted()
    {
        var h = await Shop();
        h.Tasks.StartSketch();

        h.Invoking(x => x.Tasks.DeletePort(x.Port("User").Id)).Should().Throw<ProjectRuleException>();
    }

    [Fact]
    public async Task Every_new_driving_port_starts_with_Specs_and_driven_ports_start_empty()
    {
        var h = await Shop();
        h.Tasks.StartSketch();

        var driving = h.Tasks.AddPort(Side.Driving);
        var driven = h.Tasks.AddPort(Side.Driven);

        h.Queries.Sketch!.Port(driving)!.Adapters.Should().ContainSingle().Which.Should()
            .BeEquivalentTo(new { Name = "Specs", Kind = AdapterKind.Checks, Technology = "xUnit" });
        h.Queries.Sketch!.Port(driven)!.Adapters.Should().BeEmpty();
    }

    [Fact]
    public async Task Fakes_are_named_after_their_port()
    {
        var h = await Shop();
        h.Tasks.StartSketch();
        var port = h.Tasks.PlaceOnNewPort(h.Element("Orders DB").Id, Side.Driven);
        var portId = h.Queries.Sketch!.Ports.Single(p => p.Adapters.Any(a => a.Id == port)).Id;
        h.Tasks.RenamePort(portId, "Orders");

        h.Tasks.AddAdapter(portId, AdapterKind.Fake);
        h.Tasks.AddAdapter(portId, AdapterKind.Simulated);

        h.Queries.Sketch!.Port(portId)!.Adapters.Select(a => a.Name).Should().Equal("Orders DB", "Fake Orders", "Simulated Orders");
    }

    [Fact]
    public async Task Checks_belong_on_driving_ports_only()
    {
        var h = await Shop();
        h.Tasks.StartSketch();
        var driven = h.Tasks.AddPort(Side.Driven);
        var real = h.Tasks.AddAdapter(driven);

        h.Invoking(x => x.Tasks.AddAdapter(driven, AdapterKind.Checks)).Should().Throw<ProjectRuleException>();
        h.Invoking(x => x.Tasks.SetAdapterKind(real, AdapterKind.Checks)).Should().Throw<ProjectRuleException>();
        h.Invoking(x => x.Tasks.MoveAdapter(x.Adapter("Specs").Id, driven)).Should().Throw<ProjectRuleException>();
    }

    [Fact]
    public async Task Placing_an_item_on_a_port_makes_an_adapter_named_after_it()
    {
        var h = await Shop();
        h.Tasks.StartSketch();
        var port = h.Tasks.AddPort(Side.Driven);

        var adapter = h.Tasks.Place(h.Element("Payments").Id, port);

        h.Queries.Sketch!.Adapter(adapter)!.Should().BeEquivalentTo(new { Name = "Payments", Kind = AdapterKind.Real });
        h.Queries.Sketch!.Adapter(adapter)!.Sources.Select(s => s.Name).Should().Equal("Payments");
        h.UnplacedNames.Should().Equal("Orders DB");
    }

    [Fact]
    public async Task Dropping_an_item_on_the_hexagon_makes_a_port_named_after_it_on_that_side()
    {
        var h = await Shop();
        h.Tasks.StartSketch();

        h.Tasks.PlaceOnNewPort(h.Element("Payments").Id, Side.Driven);

        var port = h.Port("Payments");
        port.Side.Should().Be(Side.Driven);
        port.Adapters.Select(a => a.Name).Should().Equal("Payments");
    }

    [Fact]
    public async Task Dropping_an_item_on_an_adapter_merges_it_in()
    {
        var h = await Shop();
        h.Tasks.StartSketch();
        h.Tasks.RemoveAdapter(h.Adapter("Staff").Id);

        h.Tasks.MergeInto(h.Element("Staff").Id, h.Adapter("Customer").Id);

        h.Adapter("Customer").Sources.Select(s => s.Name).Should().Equal("Customer", "Staff");
        h.UnplacedNames.Should().NotContain("Staff");
    }

    [Fact]
    public async Task An_item_can_be_the_source_of_only_one_adapter()
    {
        var h = await Shop();
        h.Tasks.StartSketch();

        h.Invoking(x => x.Tasks.MergeInto(x.Element("Staff").Id, x.Adapter("Customer").Id)).Should().Throw<ProjectRuleException>();
        h.Invoking(x => x.Tasks.Place(x.Element("Customer").Id, x.Port("User").Id)).Should().Throw<ProjectRuleException>();
        h.Invoking(x => x.Tasks.Place(x.System.Id, x.Port("User").Id)).Should().Throw<ProjectRuleException>();
    }

    [Fact]
    public async Task Removing_an_adapter_or_one_of_its_sources_returns_the_item_to_the_unplaced_list()
    {
        var h = await Shop();
        h.Tasks.StartSketch();
        h.Tasks.RemoveAdapter(h.Adapter("Staff").Id);
        h.Tasks.MergeInto(h.Element("Staff").Id, h.Adapter("Customer").Id);

        h.Tasks.RemoveSource(h.Adapter("Customer").Id, h.Element("Staff").Id);
        h.UnplacedNames.Should().Contain("Staff");

        h.Tasks.RemoveAdapter(h.Adapter("Customer").Id);
        h.UnplacedNames.Should().Contain("Customer");
    }

    [Fact]
    public async Task Deleting_a_port_returns_its_items_to_the_unplaced_list()
    {
        var h = await Shop();
        h.Tasks.StartSketch();
        h.Tasks.PlaceOnNewPort(h.Element("Orders DB").Id, Side.Driven);

        h.Tasks.DeletePort(h.Port("Orders DB").Id);

        h.UnplacedNames.Should().Contain("Orders DB");
    }

    [Fact]
    public async Task Ports_and_adapters_can_be_reordered_but_ports_stay_on_their_side()
    {
        var h = await Shop();
        h.Tasks.StartSketch();
        var a = h.Tasks.AddPort(Side.Driven);
        var b = h.Tasks.AddPort(Side.Driven);
        h.Tasks.AddPort(Side.Driving);

        h.Tasks.MovePort(b, 0);
        h.Queries.Sketch!.Ports.Where(p => p.Side == Side.Driven).Select(p => p.Id).Should().Equal(b, a);

        h.Tasks.MoveAdapter(h.Adapter("Staff").Id, h.Port("User").Id, 0);
        h.Port("User").Adapters.Select(x => x.Name).Should().Equal("Staff", "Customer", "Specs");

        h.Tasks.MoveAdapter(h.Adapter("Staff").Id, a);
        h.Queries.Sketch!.Port(a)!.Adapters.Select(x => x.Name).Should().Equal("Staff");
    }

    [Fact]
    public async Task Adapters_and_ports_have_their_own_names_technology_descriptions_and_kinds()
    {
        var h = await Shop();
        h.Tasks.StartSketch();
        var id = h.Adapter("Customer").Id;

        h.Tasks.RenameAdapter(id, "Web App");
        h.Tasks.SetAdapterTechnology(id, "Blazor");
        h.Tasks.DescribeAdapter(id, "The shop front");
        h.Tasks.SetAdapterKind(id, AdapterKind.Simulated);
        h.Tasks.DescribePort(h.Port("User").Id, "People");

        h.Queries.Sketch!.Adapter(id)!.Should().BeEquivalentTo(new
        {
            Name = "Web App", Technology = "Blazor", Description = "The shop front", Kind = AdapterKind.Simulated,
            Caption = "Simulated · Blazor",
        });
        h.Port("User").Description.Should().Be("People");
        h.Element("Customer").Name.Should().Be("Customer");
    }
}
