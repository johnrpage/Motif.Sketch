using System.Text;

namespace Motif.Sketch;

/// <summary>Writes a <see cref="SolutionPlan"/> as one Markdown file with fixed headings, for a coding agent.</summary>
public static class PlanMarkdown
{
    public static string Write(SolutionPlan plan)
    {
        var md = new StringBuilder();
        var s = plan.Solution;
        var useCases = plan.Project(ProjectRole.UseCases).Name;
        var projects = Ordered(plan).ToList();
        void Line(string text = "") => md.Append(text).Append('\n');

        Line($"# {plan.SystemName}: solution plan");
        Line();
        Line($"*Exported by Motif Sketch from the project \"{plan.Title}\". Stubs only: no functionality.*");
        Line();

        Line("## 1. Overview");
        Line();
        Line($"- **System**: {plan.SystemName}{Suffix(plan.SystemTechnology)}");
        if (!string.IsNullOrWhiteSpace(plan.SystemDescription))
            Line($"- **Purpose**: {plan.SystemDescription}");
        var wpf = projects.Any(p => p.Template == "wpf") ? $" (`{SolutionPlanner.TargetFramework}-windows` for WPF projects)" : "";
        Line($"- **Solution**: `{s}.sln`, targeting `{SolutionPlanner.TargetFramework}`{wpf}");
        Line("- **Architecture**: ports and adapters. The application core (Domain and UseCases) knows nothing about the outside world; every external concern sits behind a port and is reached through an adapter.");
        Line($"- **Ports**: {Count(plan.Ports, Side.Driving)} driving, {Count(plan.Ports, Side.Driven)} driven");
        Line();

        Line("## 2. Solution and project layout");
        Line();
        Line("Projects are grouped by role, and the solution folders in the `.sln` mirror the folders on disk. There are no `src/` or `tests/` folders.");
        Line();
        Line("```");
        Line($"{s}.sln");
        string? folder = null;
        foreach (var project in projects)
        {
            if (project.SolutionFolder is { } f && f != folder)
                Line($"  {f}/");
            folder = project.SolutionFolder;
            Line(folder is null ? $"  {project.Name}/" : $"    {project.Name}/");
        }
        Line("```");
        Line();
        Line("| Project | Folder | Template | Namespace | Role |");
        Line("|---|---|---|---|---|");
        foreach (var project in projects)
            Line($"| `{project.Name}` | `{project.Folder}/` | `{project.Template}` | `{project.Namespace}` | {RoleText(plan, project)} |");
        Line();

        Line("## 3. Project references (dependency rules)");
        Line();
        Line("| Project | References |");
        Line("|---|---|");
        foreach (var project in projects)
            Line($"| `{project.Name}` | {(project.References.Count == 0 ? "nothing" : string.Join(", ", project.References.Select(r => $"`{r}`")))} |");
        Line();
        Line("Rules:");
        Line();
        Line($"- `{s}.Domain` references nothing.");
        Line($"- `{useCases}` references `{s}.Domain` only.");
        Line($"- Every adapter project references `{useCases}` only. **Adapters never reference each other.**");
        Line("- The specs project may also reference the fakes it wires up by hand.");
        Line("- These references are the dependency rule. Don't add others.");
        Line();

        Line("## 4. Namespaces");
        Line();
        Line($"- `{s}.Domain` and `{useCases}` both use the plain `{s}` namespace, so the port interfaces are `{s}.{plan.Ports[0].Interfaces[0]}` and so on.");
        Line("- Every other project uses the namespace in section 2. Real and simulated adapters share one namespace, and so do the fakes.");
        Line("- Folders inside a project don't change its namespace.");
        Line("- Set `RootNamespace` in each `.csproj` to its namespace, so new files created in an IDE get it.");
        Line($"- Every namespace sits inside `{s}`, so adapters, fakes and specs see the port interfaces without a `using`.");
        Line();

        Line("## 5. Ports");
        Line();
        Line($"All port interfaces live in `{useCases}`, namespace `{s}`, one file each. Interfaces take the plain name of the port, **with no `I` prefix**. Leave them empty: their members are design work still to do.");
        Line();
        Line("```");
        Line($"{useCases}/");
        foreach (var name in plan.Ports.Where(p => p.Folder == "").SelectMany(p => p.Interfaces))
            Line($"  {name}.cs");
        Line($"  {plan.ApplicationClass}.cs");
        foreach (var group in plan.Ports.Where(p => p.Folder != "").GroupBy(p => p.Folder))
        {
            Line($"  {group.Key}/");
            foreach (var name in group.SelectMany(p => p.Interfaces))
                Line($"    {name}.cs");
        }
        Line("```");
        Line();
        foreach (var port in plan.Ports)
        {
            Line($"### {port.Name} ({(port.Side == Side.Driving ? "driving" : "driven")})");
            Line();
            if (port.IsUser)
                Line("The User port: how people (and anything acting for them) use the application. It has two halves, commands and queries.");
            if (!string.IsNullOrWhiteSpace(port.Description))
                Line(port.Description);
            if (port.IsUser || !string.IsNullOrWhiteSpace(port.Description))
                Line();
            foreach (var name in port.Interfaces)
            {
                Line($"`{(port.Folder == "" ? "" : port.Folder + "/")}{name}.cs`:");
                Line();
                Line("```csharp");
                Line($"namespace {s};");
                Line();
                Line($"public interface {name}");
                Line("{");
                Line("}");
                Line("```");
                Line();
            }
        }
        var drivingInterfaces = plan.Ports.Where(p => p.Side == Side.Driving).SelectMany(p => p.Interfaces).ToList();
        Line($"Also add one stub class, `{plan.ApplicationClass}` in `{useCases}` (namespace `{s}`), implementing the driving interfaces ({string.Join(", ", drivingInterfaces.Select(i => $"`{i}`"))}). It is the application itself; its constructor will later take the driven ports.");
        Line();

        Line("## 6. Adapters");
        Line();
        Line("Kinds: **Real** is the production adapter; **Fake** is a simple in-process stand-in for fast specs; **Simulated** is a higher-fidelity stand-in for people running the whole app locally; **Checks** drive the application through a driving port and check what it does.");
        Line();
        Line("| Port | Adapter | Kind | Technology | Project | Class | Source |");
        Line("|---|---|---|---|---|---|---|");
        foreach (var port in plan.Ports)
            foreach (var a in port.Adapters)
                Line($"| {port.Name} | {a.Name} | {a.Kind} | {a.Technology ?? "-"} | `{a.Project}`{(a.Folder == "" ? "" : $" (`{a.Folder}/`)")} | {(a.Class is null ? "-" : $"`{a.Class}`")} | {a.Source} |");
        Line();
        Line("For each driven adapter, add the stub class named above implementing its port's interface, in its project's namespace and in the folder shown. For example:");
        Line();
        var example = plan.Ports.Where(p => p.Side == Side.Driven)
            .SelectMany(p => p.Adapters.Select(a => (Port: p, Adapter: a)))
            .OrderBy(x => x.Adapter.Kind != AdapterKind.Real)
            .FirstOrDefault(x => x.Adapter.Class is not null);
        Line("```csharp");
        if (example.Adapter is { } adapter)
        {
            Line($"namespace {plan.Projects.First(p => p.Name == adapter.Project).Namespace};");
            Line();
            Line($"public sealed class {adapter.Class} : {example.Port.Interfaces[0]}");
        }
        else
        {
            Line($"namespace {s}.Adapters;");
            Line();
            Line("public sealed class FileSystemStorage : Storage");
        }
        Line("{");
        Line("}");
        Line("```");
        Line();
        Line("Driving adapters (the app among them) keep their project template's default files. Checks and fakes on driving ports are the specs (section 7).");
        Line();
        var descriptions = plan.Ports.SelectMany(p => p.Adapters).Where(a => !a.AutoAdded && !string.IsNullOrWhiteSpace(a.Description)).ToList();
        if (descriptions.Count > 0)
        {
            Line("Adapter notes:");
            Line();
            foreach (var a in descriptions)
                Line($"- **{a.Name}** (`{a.Project}`): {a.Description}");
            Line();
        }

        Line("## 7. Fakes and specs");
        Line();
        if (plan.Projects.FirstOrDefault(p => p.Role == ProjectRole.Fakes) is { } fakes)
        {
            var folders = plan.Ports.SelectMany(p => p.Adapters).Where(a => a.Project == fakes.Name).Select(a => $"`{a.Folder}/`").Distinct();
            Line($"- `{fakes.Name}` (`{fakes.Folder}/`): every fake driven adapter, with a folder per port ({string.Join(", ", folders)}). The namespace is flat: `{fakes.Namespace}`.");
        }
        var specs = plan.Project(ProjectRole.Specs);
        Line($"- `{specs.Name}` (`{specs.Folder}/`): xUnit. Packages: {string.Join(", ", specs.Packages.Select(p => $"`{p}`"))}. Keep the template's packages and add `AwesomeAssertions`.");
        Line("  Add one placeholder spec class with a single passing `[Fact]`, so `dotnet test` runs. Specs wire the fake adapters up by hand; there is no composition root yet.");
        var browser = plan.Projects.Where(p => p.Template == "razorclasslib").ToList();
        if (browser.Count > 0)
            Line($"- The browser adapters ({string.Join(", ", browser.Select(p => $"`{p.Name}`"))}) also take `Microsoft.JSInterop`: each reaches the browser through a small JS module it ships as a static web asset.");
        Line("- Use the latest stable package versions. Use AwesomeAssertions (`.Should()` style) in specs.");
        Line();

        Line("## 8. Not placed");
        Line();
        if (plan.NotPlaced.Count == 0)
            Line("Every element of the context diagram is on the sketch.");
        else
        {
            Line("These context diagram elements were deliberately left off the sketch. Don't build anything for them.");
            Line();
            foreach (var element in plan.NotPlaced)
                Line($"- {element.Name} ({TypeText(element.Type)}{Suffix(element.Technology, ", ")})");
        }
        Line();

        Line("## 9. Build this");
        Line();
        Line("Instructions for the coding agent:");
        Line();
        Line($"1. Create `{s}.sln` (classic `.sln` format: `dotnet new sln --format sln`).");
        Line("2. Create every project in section 2 with its template, in its folder, and add it to the solution in the matching solution folder (`dotnet sln add <project> --solution-folder <folder>`, or `--in-root` for top-level projects). Set each project's `RootNamespace`.");
        var samples = browser.Count > 0 ? ", and the `razorclasslib` samples (`Component1.razor`, `ExampleJsInterop.cs`, the files in `wwwroot/`)" : "";
        Line($"3. Delete template placeholder files (`Class1.cs`, `UnitTest1.cs`{samples}), except `Class1.cs` in `{s}.Domain`: keep it, with its namespace changed to `{s}`, to show the namespace choice.");
        Line("4. Add exactly the project references in section 3, and the packages in section 7.");
        Line("5. Add a root `.editorconfig` with a naming rule that says interfaces are plain PascalCase, so the IDE doesn't ask for an `I` prefix:");
        Line();
        Line("   ```ini");
        foreach (var line in EditorConfig)
            Line(line.Length == 0 ? "" : $"   {line}");
        Line("   ```");
        Line();
        Line("6. Add the port interfaces and the application stub from section 5, and the adapter stub classes from section 6.");
        Line("7. Add the placeholder spec from section 7.");
        Line("8. Check that `dotnet build` and `dotnet test` both succeed.");
        Line();
        Line("Ground rules:");
        Line();
        Line("- **Stubs only.** No functionality, no interface members, no wiring.");
        Line("- **No composition root** (no host project, no dependency injection setup) in this version.");
        Line("- **No `I` prefix** on interfaces. Implementations carry the meaningful extra (`FileSystemStorage`), never `Impl`.");
        Line("- **Two-letter acronyms stay in capitals** in code names (`IndexedDB`, `UI`).");
        Line("- Don't add projects, references or packages the plan doesn't list.");

        return md.ToString();
    }

    private static readonly string[] EditorConfig =
    [
        "root = true",
        "",
        "[*.cs]",
        "dotnet_naming_rule.interfaces_no_prefix.symbols = interfaces",
        "dotnet_naming_rule.interfaces_no_prefix.style = pascal_case",
        "dotnet_naming_rule.interfaces_no_prefix.severity = warning",
        "",
        "dotnet_naming_symbols.interfaces.applicable_kinds = interface",
        "dotnet_naming_symbols.interfaces.applicable_accessibilities = *",
        "",
        "dotnet_naming_style.pascal_case.capitalization = pascal_case",
    ];

    /// <summary>Top-level projects first, then each solution folder's projects together.</summary>
    private static IEnumerable<PlannedProject> Ordered(SolutionPlan plan) =>
        plan.Projects.Where(p => p.SolutionFolder is null)
            .Concat(plan.Projects.Where(p => p.SolutionFolder is not null).GroupBy(p => p.SolutionFolder).SelectMany(g => g));

    private static int Count(IEnumerable<PlannedPort> ports, Side side) => ports.Count(p => p.Side == side);

    private static string Suffix(string? text, string separator = " ") =>
        string.IsNullOrWhiteSpace(text) ? "" : separator == " " ? $" ({text})" : $"{separator}{text}";

    private static string RoleText(SolutionPlan plan, PlannedProject project) => project.Role switch
    {
        ProjectRole.Domain => "Domain-specific business rules",
        ProjectRole.UseCases => "Application rules (use cases) and all port interfaces",
        ProjectRole.App => "The app: runnable adapter on the User port",
        ProjectRole.Adapters =>
            $"Adapters on the {plan.Ports.First(p => p.Adapters.Any(a => a.Project == project.Name)).Name} port",
        ProjectRole.Fakes => "Fake driven adapters, for specs",
        _ => "Specs: the checks on every driving port",
    };

    private static string TypeText(ElementType type) => type switch
    {
        ElementType.PrimaryActor => "primary actor",
        ElementType.SecondaryActor => "secondary actor",
        ElementType.ExternalSystem => "external system",
        ElementType.Storage => "storage",
        _ => "system",
    };
}
