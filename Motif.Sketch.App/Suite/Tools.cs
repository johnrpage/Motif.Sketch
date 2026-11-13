// Motif.NET shared web kit: copied from github.com/motif-dotnet/.github/tree/main/brand/web. Don't edit an app's copy.
namespace Motif.Suite;

/// <param name="Id">Short and stable: what an app passes to the tool switcher as itself.</param>
/// <param name="Name">The tool's name after "Motif", as in its wordmark.</param>
/// <param name="Line">One line on what it's for, shown in the tool switcher.</param>
/// <param name="Ready">False for a tool that's next up but not built yet: the switcher shows it greyed out, with no link.</param>
/// <param name="NewUntil">
/// While set and not past, the tool is news: the switcher tags it "new", and puts a dot on the Motif mark
/// until the viewer next opens the menu. Leave it unset once the news is old.
/// </param>
/// <param name="Preview">
/// True while the tool is an early version that may change and have bugs: the brand shows a "preview" tag, and
/// the switcher says what that means, with a link to the tool's issues.
/// </param>
public sealed record Tool(string Id, string Name, string Line, string Url, bool Ready = true, DateOnly? NewUntil = null, bool Preview = false)
{
    public bool IsNew(DateOnly today) => NewUntil is { } until && today <= until;

    /// <summary>Where to report a bug: the tool's issues on GitHub.</summary>
    public string Issues => $"https://github.com/motif-dotnet/Motif.{Name}/issues";
}

/// <summary>The Motif tools that run in the browser, in the order the tool switcher lists them.</summary>
public static class Tools
{
    /// <summary>Motif.NET's home: the suite's landing page, which lists every tool.</summary>
    public const string Home = "https://motif-dotnet.github.io/";

    public static readonly IReadOnlyList<Tool> All =
    [
        new("sketch", "Sketch", "Sketch a context diagram and its ports and adapters, then export a solution plan.", "https://motif-dotnet.github.io/Motif.Sketch/", Preview: true),
        new("glossary", "Glossary", "Review a project's glossary at a glance.", "https://motif-dotnet.github.io/Motif.Glossary/", Preview: true),
        new("memory", "Memory", "Set up an agent's external memory, with the roles and workflow that suit you.", "https://motif-dotnet.github.io/Motif.Memory/", NewUntil: new(2026, 12, 31), Preview: true),
    ];

    public static Tool Get(string id) => All.First(t => t.Id == id);

    /// <summary>
    /// A link to the tool. With a repo ("owner/name") it opens that repo's <c>motif/</c> folder:
    /// <c>#repo=owner/name</c> is how every Motif tool is told which project to show.
    /// </summary>
    public static string Link(Tool tool, string? repo) =>
        string.IsNullOrEmpty(repo) ? tool.Url : $"{tool.Url}#repo={repo}";
}
