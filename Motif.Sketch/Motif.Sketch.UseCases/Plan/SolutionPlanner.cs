using System.Text.RegularExpressions;

namespace Motif.Sketch;

/// <summary>
/// The plan's opinions, hard-coded: the core (Domain and UseCases) shares the root namespace with
/// the port interfaces; each port's real and simulated adapters share one project under Adapters/;
/// the User port's one runnable adapter is the App; fakes share one project; checks and driving-port
/// fakes are the Specs; every port gets a fake or checks.
/// </summary>
public static partial class SolutionPlanner
{
    public const string TargetFramework = "net10.0";

    /// <summary>The xunit template's packages (coverlet.collector included) plus the assertion library.</summary>
    public static readonly IReadOnlyList<string> SpecPackages =
        ["Microsoft.NET.Test.Sdk", "xunit", "xunit.runner.visualstudio", "coverlet.collector", "AwesomeAssertions"];

    /// <summary>Adapters in a Blazor WebAssembly solution reach the browser through JS interop.</summary>
    public static readonly IReadOnlyList<string> BrowserAdapterPackages = ["Microsoft.JSInterop"];

    /// <summary>
    /// Ports can't take these names: each is a project or a namespace beside the port interfaces, and
    /// "Application" would hide the UI framework's Application class from the App.
    /// </summary>
    public static readonly IReadOnlyList<string> ReservedNames =
        ["Domain", "UseCases", "Application", "App", "Adapters", "Fakes", "Specs"];

    public static SolutionPlan Plan(Project project)
    {
        var problems = new List<string>();
        var warnings = new List<string>();
        var system = project.ContextDiagram.System;
        var sketch = project.Sketch ?? throw new InvalidOperationException("The plan needs a sketch.");
        var s = CodeName.Root(system.CodeRoot ?? system.Name);
        if (!CodeName.IsValid(s))
            problems.Add(system.CodeRoot is { } root
                ? $"The code root '{root}' doesn't make a usable solution name."
                : $"The system name '{system.Name}' doesn't make a usable solution name. Give the system a code root.");

        var ports = sketch.Ports.OrderBy(p => p.Side).ToList();
        var userRunnable = sketch.UserPort.Adapters.Where(IsRunnable).ToList();
        var blazor = userRunnable.Any(a => TemplateFor(Side.Driving, a.Technology, false) == "blazorwasm");
        var planned = ports.ToDictionary(p => p, p => AdaptersFor(project, p, s, userRunnable.Count, problems));

        var domain = new PlannedProject($"{s}.Domain", $"{s}/{s}.Domain", "classlib", s, ProjectRole.Domain, [], []);
        var useCases = new PlannedProject($"{s}.UseCases", $"{s}/{s}.UseCases", "classlib", s, ProjectRole.UseCases, [domain.Name], []);
        var projects = new List<PlannedProject> { domain, useCases };

        foreach (var (port, adapters) in planned)
        {
            var plugged = port.Adapters.Zip(adapters).Where(x => IsRunnable(x.First)).ToList();
            if (port.IsUser)
            {
                projects.AddRange(plugged.Select(x => new PlannedProject(
                    x.Second.Project, x.Second.Project, TemplateFor(Side.Driving, x.First.Technology, blazor),
                    x.Second.Project, ProjectRole.App, [useCases.Name], [])));
            }
            else if (plugged.Count > 0)
            {
                var templates = plugged.Select(x => TemplateFor(port.Side, x.First.Technology, blazor)).Distinct().ToList();
                if (templates.Count > 1)
                    problems.Add($"The adapters on port '{port.Name}' share a project but need different project templates ({string.Join(", ", templates)}).");
                var name = plugged[0].Second.Project;
                var packages = templates[0] == "razorclasslib" ? BrowserAdapterPackages : [];
                projects.Add(new(name, $"Adapters/{name}", templates[0], $"{s}.Adapters", ProjectRole.Adapters, [useCases.Name], packages));
            }
        }

        var specsReferences = new List<string> { useCases.Name };
        if (ports.Any(p => p.Side == Side.Driven))
        {
            projects.Add(new($"{s}.Fakes", $"Checks/{s}.Fakes", "classlib", $"{s}.Fakes", ProjectRole.Fakes, [useCases.Name], []));
            specsReferences.Add($"{s}.Fakes");
        }
        projects.Add(new($"{s}.Specs", $"Checks/{s}.Specs", "xunit", $"{s}.Specs", ProjectRole.Specs, specsReferences, SpecPackages));

        var appNames = projects.Where(p => p.Role == ProjectRole.App && userRunnable.Count > 1).Select(p => p.Name[(s.Length + 1)..]);
        CheckNames(s, ports, planned, [.. ReservedNames, .. appNames, .. s.Split('.'), s.Replace(".", "")], problems);
        foreach (var clash in projects.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            problems.Add($"More than one project would be called '{clash.Key}'. Rename a port or adapter.");

        var unplaced = project.Unplaced;
        if (unplaced.Count > 0)
            warnings.Add(unplaced.Count == 1
                ? $"'{unplaced[0].Name}' isn't placed. It's listed under \"Not placed\" in the plan."
                : $"{unplaced.Count} items aren't placed. They're listed under \"Not placed\" in the plan.");
        foreach (var adapter in sketch.Adapters.Where(a => a.SourceDeleted))
            warnings.Add($"'{adapter.Name}' stood for a context diagram element that has been deleted.");

        var plannedPorts = ports.Select(p => new PlannedPort(
            p.Name, p.Description, p.Side, p.IsUser, InterfacesFor(p), p.IsUser ? "" : "Ports", planned[p])).ToList();

        return new SolutionPlan(s, project.Title, system.Name, system.Description, system.Technology,
            projects, plannedPorts, unplaced, warnings, problems);
    }

    /// <summary>The User port is two interfaces (tasks and queries); every other port is one.</summary>
    public static IReadOnlyList<string> InterfacesFor(Port port) =>
        port.IsUser ? ["UserTasks", "UserQueries"] : [CodeName.From(port.Name)];

    /// <summary>
    /// Implementations carry the meaningful extra, taken from the adapter's name ("Browser" on port
    /// "Clipboard" is BrowserClipboard). When the name only repeats the port's, or is empty, the
    /// technology stands in, and failing that the kind (FakeClipboard). Null when a real adapter has neither.
    /// </summary>
    public static string? ClassFor(string portCode, string adapterCode, string? technology, AdapterKind kind)
    {
        var tech = CodeName.From(Brackets().Replace(technology ?? "", " "));
        var prefix =
            CodeName.IsValid(adapterCode) && !adapterCode.Equals(portCode, StringComparison.OrdinalIgnoreCase) ? adapterCode
            : CodeName.IsValid(tech) ? tech
            : kind != AdapterKind.Real ? kind.ToString()
            : null;
        return prefix is null ? null
            : prefix.EndsWith(portCode, StringComparison.Ordinal) ? prefix
            : prefix + portCode;
    }

    private static bool IsRunnable(Adapter adapter) => adapter.Kind is AdapterKind.Real or AdapterKind.Simulated;

    private static List<PlannedAdapter> AdaptersFor(Project project, Port port, string s, int runnable, List<string> problems)
    {
        var portCode = CodeName.From(port.Name);
        var result = new List<PlannedAdapter>();

        foreach (var adapter in port.Adapters)
        {
            var code = CodeName.From(adapter.Name);
            if (!CodeName.IsValid(code))
                problems.Add($"The adapter name '{adapter.Name}' on port '{port.Name}' doesn't make a usable code name.");

            var names = adapter.Sources.Select(project.ContextDiagram.Find).OfType<Element>().Select(e => e.Name).ToList();
            var source = names.Count > 0 ? $"Context diagram: {string.Join(", ", names)}" : "Added in the sketch";
            var cls = port.Side == Side.Driven ? ClassFor(portCode, code, adapter.Technology, adapter.Kind) : null;
            if (port.Side == Side.Driven && cls is null)
                problems.Add($"The adapter '{adapter.Name}' on port '{port.Name}' needs a name of its own or a technology, to name its class.");

            var (proj, folder) = adapter.Kind switch
            {
                AdapterKind.Checks or AdapterKind.Fake when port.Side == Side.Driving => ($"{s}.Specs", ""),
                AdapterKind.Fake => ($"{s}.Fakes", portCode),
                _ when port.IsUser => (runnable == 1 ? $"{s}.App" : $"{s}.{code}", ""),
                _ => ($"{s}.{portCode}", ""),
            };
            result.Add(new PlannedAdapter(adapter.Name, adapter.Kind, adapter.Technology, adapter.Description,
                proj, cls, folder, source, AutoAdded: false));
        }

        var driving = port.Side == Side.Driving;
        var covered = driving
            ? port.Adapters.Any(a => a.Kind is AdapterKind.Checks or AdapterKind.Fake)
            : port.Adapters.Any(a => a.Kind == AdapterKind.Fake);
        if (!covered)
        {
            result.Add(new PlannedAdapter(
                driving ? Project.SpecsName : $"Fake {port.Name}",
                driving ? AdapterKind.Checks : AdapterKind.Fake,
                driving ? Project.SpecsTechnology : null,
                driving
                    ? $"Specs that drive the application through the {port.Name} port, using the fake driven adapters."
                    : $"In-memory stand-in for the {port.Name} port, for fast specs.",
                driving ? $"{s}.Specs" : $"{s}.Fakes",
                driving ? null : ClassFor(portCode, "Fake", null, AdapterKind.Fake),
                driving ? "" : portCode,
                "Added by the plan: every port gets a fake or checks", AutoAdded: true));
        }
        return result;
    }

    private static void CheckNames(
        string s, List<Port> ports, Dictionary<Port, List<PlannedAdapter>> planned,
        IReadOnlyList<string> reserved, List<string> problems)
    {
        foreach (var port in ports.Where(p => !CodeName.IsValid(CodeName.From(p.Name))))
            problems.Add($"The port name '{port.Name}' doesn't make a usable code name.");

        var interfaces = ports.SelectMany(InterfacesFor).Where(n => n.Length > 0).Append($"{s.Replace(".", "")}Application").ToList();
        foreach (var clash in interfaces.GroupBy(n => n, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            problems.Add($"More than one port would make an interface called '{clash.Key}'.");

        foreach (var port in ports.Where(p => !p.IsUser && reserved.Contains(CodeName.From(p.Name), StringComparer.OrdinalIgnoreCase)))
            problems.Add($"A port can't be called '{port.Name}': that name is taken by a project or namespace in the plan.");

        var classes = planned.Values.SelectMany(a => a).Where(a => a.Class is not null).ToList();
        foreach (var clash in classes.GroupBy(a => (a.Project == $"{s}.Fakes", a.Class), (key, g) => (key.Class, Count: g.Count()))
                     .Where(g => g.Count > 1))
            problems.Add($"More than one adapter would make a class called '{clash.Class}'. Rename an adapter.");

        foreach (var adapter in classes.Where(a => interfaces.Contains(a.Class!, StringComparer.OrdinalIgnoreCase)))
            problems.Add($"The adapter '{adapter.Name}' would make a class called '{adapter.Class}', the same as a port interface. Rename it or give it a technology.");
    }

    /// <summary>The project template an adapter needs, from its technology.</summary>
    public static string TemplateFor(Side side, string? technology, bool blazorSolution)
    {
        var tech = technology?.ToLowerInvariant() ?? "";
        if (side == Side.Driving)
        {
            if (tech.Contains("blazor")) return "blazorwasm";
            if (tech.Contains("wpf")) return "wpf";
            if (tech.Contains("cli") || tech.Contains("console")) return "console";
            if (tech.Contains("web") || tech.Contains("http")) return "web";
            return "classlib";
        }
        // In a browser app, a driven adapter reaches the browser through a JS module it ships as a static web asset.
        return blazorSolution ? "razorclasslib" : "classlib";
    }

    [GeneratedRegex(@"\([^)]*\)")]
    private static partial Regex Brackets();
}
