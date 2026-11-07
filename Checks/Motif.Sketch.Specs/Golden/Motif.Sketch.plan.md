# Motif.Sketch: solution plan

*Exported by Motif Sketch from the project "Motif Sketch". Stubs only: no functionality.*

## 1. Overview

- **System**: Motif.Sketch (Blazor WebAssembly (GitHub Pages))
- **Purpose**: Sketch a context diagram, turn it into a ports-and-adapters sketch, then export the diagrams and a solution plan
- **Solution**: `Motif.Sketch.sln`, targeting `net10.0`
- **Architecture**: ports and adapters. The application core (Domain and UseCases) knows nothing about the outside world; every external concern sits behind a port and is reached through an adapter.
- **Ports**: 1 driving, 5 driven

## 2. Solution and project layout

Projects are grouped by role, and the solution folders in the `.sln` mirror the folders on disk. There are no `src/` or `tests/` folders.

```
Motif.Sketch.sln
  Motif.Sketch.App/
  Motif.Sketch/
    Motif.Sketch.Domain/
    Motif.Sketch.UseCases/
  Adapters/
    Motif.Sketch.Projects/
    Motif.Sketch.Autosave/
    Motif.Sketch.DiagramExports/
    Motif.Sketch.SolutionStubs/
    Motif.Sketch.Clipboard/
  Checks/
    Motif.Sketch.Fakes/
    Motif.Sketch.Specs/
```

| Project | Folder | Template | Namespace | Role |
|---|---|---|---|---|
| `Motif.Sketch.App` | `Motif.Sketch.App/` | `blazorwasm` | `Motif.Sketch.App` | The app: runnable adapter on the User port |
| `Motif.Sketch.Domain` | `Motif.Sketch/Motif.Sketch.Domain/` | `classlib` | `Motif.Sketch` | Domain-specific business rules |
| `Motif.Sketch.UseCases` | `Motif.Sketch/Motif.Sketch.UseCases/` | `classlib` | `Motif.Sketch` | Application rules (use cases) and all port interfaces |
| `Motif.Sketch.Projects` | `Adapters/Motif.Sketch.Projects/` | `razorclasslib` | `Motif.Sketch.Adapters` | Adapters on the Projects port |
| `Motif.Sketch.Autosave` | `Adapters/Motif.Sketch.Autosave/` | `razorclasslib` | `Motif.Sketch.Adapters` | Adapters on the Autosave port |
| `Motif.Sketch.DiagramExports` | `Adapters/Motif.Sketch.DiagramExports/` | `razorclasslib` | `Motif.Sketch.Adapters` | Adapters on the Diagram Exports port |
| `Motif.Sketch.SolutionStubs` | `Adapters/Motif.Sketch.SolutionStubs/` | `razorclasslib` | `Motif.Sketch.Adapters` | Adapters on the Solution Stubs port |
| `Motif.Sketch.Clipboard` | `Adapters/Motif.Sketch.Clipboard/` | `razorclasslib` | `Motif.Sketch.Adapters` | Adapters on the Clipboard port |
| `Motif.Sketch.Fakes` | `Checks/Motif.Sketch.Fakes/` | `classlib` | `Motif.Sketch.Fakes` | Fake driven adapters, for specs |
| `Motif.Sketch.Specs` | `Checks/Motif.Sketch.Specs/` | `xunit` | `Motif.Sketch.Specs` | Specs: the checks on every driving port |

## 3. Project references (dependency rules)

| Project | References |
|---|---|
| `Motif.Sketch.App` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.Domain` | nothing |
| `Motif.Sketch.UseCases` | `Motif.Sketch.Domain` |
| `Motif.Sketch.Projects` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.Autosave` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.DiagramExports` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.SolutionStubs` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.Clipboard` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.Fakes` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.Specs` | `Motif.Sketch.UseCases`, `Motif.Sketch.Fakes` |

Rules:

- `Motif.Sketch.Domain` references nothing.
- `Motif.Sketch.UseCases` references `Motif.Sketch.Domain` only.
- Every adapter project references `Motif.Sketch.UseCases` only. **Adapters never reference each other.**
- The specs project may also reference the fakes it wires up by hand.
- These references are the dependency rule. Don't add others.

## 4. Namespaces

- `Motif.Sketch.Domain` and `Motif.Sketch.UseCases` both use the plain `Motif.Sketch` namespace, so the port interfaces are `Motif.Sketch.UserTasks` and so on.
- Every other project uses the namespace in section 2. Real and simulated adapters share one namespace, and so do the fakes.
- Folders inside a project don't change its namespace.
- Set `RootNamespace` in each `.csproj` to its namespace, so new files created in an IDE get it.
- Every namespace sits inside `Motif.Sketch`, so adapters, fakes and specs see the port interfaces without a `using`.

## 5. Ports

All port interfaces live in `Motif.Sketch.UseCases`, namespace `Motif.Sketch`, one file each. Interfaces take the plain name of the port, **with no `I` prefix**. Leave them empty: their members are design work still to do.

```
Motif.Sketch.UseCases/
  UserTasks.cs
  UserQueries.cs
  MotifSketchApplication.cs
  Ports/
    Projects.cs
    Autosave.cs
    DiagramExports.cs
    SolutionStubs.cs
    Clipboard.cs
```

### User (driving)

The User port: how people (and anything acting for them) use the application. It has two halves, commands and queries.

`UserTasks.cs`:

```csharp
namespace Motif.Sketch;

public interface UserTasks
{
}
```

`UserQueries.cs`:

```csharp
namespace Motif.Sketch;

public interface UserQueries
{
}
```

### Projects (driven)

Opens and saves project files

`Ports/Projects.cs`:

```csharp
namespace Motif.Sketch;

public interface Projects
{
}
```

### Autosave (driven)

Keeps the work in progress and its file handle between visits

`Ports/Autosave.cs`:

```csharp
namespace Motif.Sketch;

public interface Autosave
{
}
```

### Diagram Exports (driven)

Writes the context diagram and the sketch as pictures

`Ports/DiagramExports.cs`:

```csharp
namespace Motif.Sketch;

public interface DiagramExports
{
}
```

### Solution Stubs (driven)

Hands the solution stub on to be built; v2 may add a GitHub adapter that inflates it in a repo

`Ports/SolutionStubs.cs`:

```csharp
namespace Motif.Sketch;

public interface SolutionStubs
{
}
```

### Clipboard (driven)

Takes the snapshot link when the Architect copies it

`Ports/Clipboard.cs`:

```csharp
namespace Motif.Sketch;

public interface Clipboard
{
}
```

Also add one stub class, `MotifSketchApplication` in `Motif.Sketch.UseCases` (namespace `Motif.Sketch`), implementing the driving interfaces (`UserTasks`, `UserQueries`). It is the application itself; its constructor will later take the driven ports.

## 6. Adapters

Kinds: **Real** is the production adapter; **Fake** is a simple in-process stand-in for fast specs; **Simulated** is a higher-fidelity stand-in for people running the whole app locally; **Checks** drive the application through a driving port and check what it does.

| Port | Adapter | Kind | Technology | Project | Class | Source |
|---|---|---|---|---|---|---|
| User | Web App | Real | Blazor WebAssembly UI | `Motif.Sketch.App` | - | Context diagram: Architect, Viewer |
| User | Specs | Checks | xUnit | `Motif.Sketch.Specs` | - | Added in the sketch |
| Projects | Browser Files | Real | File System Access API, download fallback | `Motif.Sketch.Projects` | `BrowserFilesProjects` | Context diagram: Project Files |
| Projects | Fake Projects | Fake | - | `Motif.Sketch.Fakes` (`Projects/`) | `FakeProjects` | Added in the sketch |
| Autosave | IndexedDB | Real | Browser storage | `Motif.Sketch.Autosave` | `IndexedDBAutosave` | Context diagram: Browser Storage |
| Autosave | Fake Autosave | Fake | - | `Motif.Sketch.Fakes` (`Autosave/`) | `FakeAutosave` | Added in the sketch |
| Diagram Exports | SVG Files | Real | Browser Files (File System Access API, download fallback) | `Motif.Sketch.DiagramExports` | `SvgFilesDiagramExports` | Context diagram: Diagram Exports |
| Diagram Exports | Fake Diagram Exports | Fake | - | `Motif.Sketch.Fakes` (`DiagramExports/`) | `FakeDiagramExports` | Added in the sketch |
| Solution Stubs | Markdown Plan | Real | Browser Files (File System Access API, download fallback) | `Motif.Sketch.SolutionStubs` | `MarkdownPlanSolutionStubs` | Context diagram: Solution Plans |
| Solution Stubs | Fake Solution Stubs | Fake | - | `Motif.Sketch.Fakes` (`SolutionStubs/`) | `FakeSolutionStubs` | Added in the sketch |
| Clipboard | Browser | Real | Browser Clipboard API (JS interop) | `Motif.Sketch.Clipboard` | `BrowserClipboard` | Context diagram: Clipboard |
| Clipboard | Fake Clipboard | Fake | - | `Motif.Sketch.Fakes` (`Clipboard/`) | `FakeClipboard` | Added in the sketch |

For each driven adapter, add the stub class named above implementing its port's interface, in its project's namespace and in the folder shown. For example:

```csharp
namespace Motif.Sketch.Adapters;

public sealed class BrowserFilesProjects : Projects
{
}
```

Driving adapters (the app among them) keep their project template's default files. Checks and fakes on driving ports are the specs (section 7).

Adapter notes:

- **Web App** (`Motif.Sketch.App`): One app for the Architect (full editing) and the Viewer (read-only, opened from a snapshot link)
- **Specs** (`Motif.Sketch.Specs`): Checks that drive the application through the User port, using the fake driven adapters
- **Browser Files** (`Motif.Sketch.Projects`): Saved projects: one file per system holding the context diagram and, once started, its ports-and-adapters sketch
- **IndexedDB** (`Motif.Sketch.Autosave`): Autosaved work in progress and its file handle, so a refresh or closed tab loses nothing and Save still goes to the same file
- **SVG Files** (`Motif.Sketch.DiagramExports`): The context diagram and the ports-and-adapters sketch as pictures
- **Markdown Plan** (`Motif.Sketch.SolutionStubs`): The solution stub plan a coding agent builds from
- **Browser** (`Motif.Sketch.Clipboard`): Where Copy link puts a snapshot link, ready to paste

## 7. Fakes and specs

- `Motif.Sketch.Fakes` (`Checks/Motif.Sketch.Fakes/`): every fake driven adapter, with a folder per port (`Projects/`, `Autosave/`, `DiagramExports/`, `SolutionStubs/`, `Clipboard/`). The namespace is flat: `Motif.Sketch.Fakes`.
- `Motif.Sketch.Specs` (`Checks/Motif.Sketch.Specs/`): xUnit. Packages: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `coverlet.collector`, `AwesomeAssertions`. Keep the template's packages and add `AwesomeAssertions`.
  Add one placeholder spec class with a single passing `[Fact]`, so `dotnet test` runs. Specs wire the fake adapters up by hand; there is no composition root yet.
- The browser adapters (`Motif.Sketch.Projects`, `Motif.Sketch.Autosave`, `Motif.Sketch.DiagramExports`, `Motif.Sketch.SolutionStubs`, `Motif.Sketch.Clipboard`) also take `Microsoft.JSInterop`: each reaches the browser through a small JS module it ships as a static web asset.
- Use the latest stable package versions. Use AwesomeAssertions (`.Should()` style) in specs.

## 8. Not placed

Every element of the context diagram is on the sketch.

## 9. Build this

Instructions for the coding agent:

1. Create `Motif.Sketch.sln` (classic `.sln` format: `dotnet new sln --format sln`).
2. Create every project in section 2 with its template, in its folder, and add it to the solution in the matching solution folder (`dotnet sln add <project> --solution-folder <folder>`, or `--in-root` for top-level projects). Set each project's `RootNamespace`.
3. Delete template placeholder files (`Class1.cs`, `UnitTest1.cs`, and the `razorclasslib` samples (`Component1.razor`, `ExampleJsInterop.cs`, the files in `wwwroot/`)), except `Class1.cs` in `Motif.Sketch.Domain`: keep it, with its namespace changed to `Motif.Sketch`, to show the namespace choice.
4. Add exactly the project references in section 3, and the packages in section 7.
5. Add a root `.editorconfig` with a naming rule that says interfaces are plain PascalCase, so the IDE doesn't ask for an `I` prefix:

   ```ini
   root = true

   [*.cs]
   dotnet_naming_rule.interfaces_no_prefix.symbols = interfaces
   dotnet_naming_rule.interfaces_no_prefix.style = pascal_case
   dotnet_naming_rule.interfaces_no_prefix.severity = warning

   dotnet_naming_symbols.interfaces.applicable_kinds = interface
   dotnet_naming_symbols.interfaces.applicable_accessibilities = *

   dotnet_naming_style.pascal_case.capitalization = pascal_case
   ```

6. Add the port interfaces and the application stub from section 5, and the adapter stub classes from section 6.
7. Add the placeholder spec from section 7.
8. Check that `dotnet build` and `dotnet test` both succeed.

Ground rules:

- **Stubs only.** No functionality, no interface members, no wiring.
- **No composition root** (no host project, no dependency injection setup) in this version.
- **No `I` prefix** on interfaces. Implementations carry the meaningful extra (`FileSystemStorage`), never `Impl`.
- **Two-letter acronyms stay in capitals** in code names (`IndexedDB`, `UI`).
- Don't add projects, references or packages the plan doesn't list.
