using Motif.Sketch.Fakes;

namespace Motif.Sketch.Specs;

/// <summary>The application wired to every fake driven adapter, driven only through the User port.</summary>
public sealed class Harness
{
    public FakeProjects Projects { get; } = new();
    public FakeAutosave Autosave { get; } = new();
    public FakeDiagramExports DiagramExports { get; } = new();
    public FakeSolutionStubs SolutionStubs { get; } = new();
    public FakeClipboard Clipboard { get; } = new();

    public MotifSketchApplication App { get; }
    public UserTasks Tasks => App;
    public UserQueries Queries => App;

    public Harness() => App = new MotifSketchApplication(Projects, Autosave, DiagramExports, SolutionStubs, Clipboard);

    /// <summary>A started app with a blank project.</summary>
    public static async Task<Harness> Started(string? fragment = null)
    {
        var harness = new Harness();
        await harness.Tasks.Start(fragment);
        return harness;
    }

    /// <summary>A started app with the bundled example open.</summary>
    public static async Task<Harness> WithExample()
    {
        var harness = await Started();
        await harness.Tasks.OpenExample();
        return harness;
    }

    public ElementView Element(string name) => Queries.Context.Elements.Single(e => e.Name == name);

    public ElementView System => Queries.Context.Elements.Single(e => e.Type == ElementType.System);

    public PortShape Port(string name) => Queries.Sketch!.Ports.Single(p => p.Name == name);

    public AdapterShape Adapter(string name) => Queries.Sketch!.Ports.SelectMany(p => p.Adapters).Single(a => a.Name == name);

    public IEnumerable<AdapterShape> Adapters => Queries.Sketch!.Ports.SelectMany(p => p.Adapters);

    public IEnumerable<string> UnplacedNames => Queries.Unplaced.Select(u => u.Name);
}
