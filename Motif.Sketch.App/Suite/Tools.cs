// Motif.NET shared web kit: copied from github.com/motif-dotnet/.github/tree/main/brand/web. Don't edit an app's copy.
namespace Motif.Suite;

/// <param name="Id">Short and stable: what an app passes to the tool switcher as itself.</param>
/// <param name="Name">The tool's name after "Motif", as in its wordmark.</param>
/// <param name="Line">One line on what it's for, shown in the tool switcher.</param>
public sealed record Tool(string Id, string Name, string Line, string Url);

/// <summary>The Motif tools that run in the browser, in the order the tool switcher lists them.</summary>
public static class Tools
{
    /// <summary>Motif.NET's home. The GitHub org until the suite has a landing page.</summary>
    public const string Home = "https://github.com/motif-dotnet";

    public static readonly IReadOnlyList<Tool> All =
    [
        new("sketch", "Sketch", "Sketch a context diagram and its ports and adapters, then export a solution plan.", "https://motif-dotnet.github.io/Motif.Sketch/"),
        new("glossary", "Glossary", "Review a project's glossary at a glance.", "https://motif-dotnet.github.io/Motif.Glossary/"),
    ];

    public static Tool Get(string id) => All.First(t => t.Id == id);
}
