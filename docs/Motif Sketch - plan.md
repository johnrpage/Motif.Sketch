# Motif Sketch: solution plan

*Exported by HexaSketch from the sketch "Motif Sketch". Stubs only: no functionality.*

## 1. Overview

- **System**: Motif Sketch (Blazor WebAssembly (GitHub Pages))
- **Purpose**: Sketch a context diagram, turn it into a ports-and-adapters sketch, then export the diagrams and a solution plan
- **Solution**: `MotifSketch.sln`, targeting `net10.0` (`net10.0-windows` for WPF projects)
- **Architecture**: ports and adapters. The application core (Domain and UseCases) knows nothing about the outside world; every external concern sits behind a port and is reached through an adapter.
- **Ports**: 1 driving, 5 driven

## 2. Solution and project layout

Projects are grouped by role, and the solution folders in the `.sln` mirror the folders on disk. There are no `src/` or `tests/` folders.

```
MotifSketch.sln
  MotifSketch.App/
  MotifSketch/
    MotifSketch.Domain/
    MotifSketch.UseCases/
  Adapters/
    MotifSketch.Projects/
    MotifSketch.Autosave/
    MotifSketch.DiagramExports/
    MotifSketch.SolutionStubs/
    MotifSketch.Clipboard/
  Checks/
    MotifSketch.Fakes/
    MotifSketch.Specs/
```

| Project | Folder | Template | Namespace | Role |
|---|---|---|---|---|
| `MotifSketch.App` | `MotifSketch.App/` | `web` | `MotifSketch.App` | The app: runnable adapter on the User port |
| `MotifSketch.Domain` | `MotifSketch/MotifSketch.Domain/` | `classlib` | `MotifSketch` | Domain-specific business rules |
| `MotifSketch.UseCases` | `MotifSketch/MotifSketch.UseCases/` | `classlib` | `MotifSketch` | Application rules (use cases) and all port interfaces |
| `MotifSketch.Projects` | `Adapters/MotifSketch.Projects/` | `classlib` | `MotifSketch.Adapters` | Adapters on the Projects port |
| `MotifSketch.Autosave` | `Adapters/MotifSketch.Autosave/` | `classlib` | `MotifSketch.Adapters` | Adapters on the Autosave port |
| `MotifSketch.DiagramExports` | `Adapters/MotifSketch.DiagramExports/` | `classlib` | `MotifSketch.Adapters` | Adapters on the Diagram Exports port |
| `MotifSketch.SolutionStubs` | `Adapters/MotifSketch.SolutionStubs/` | `classlib` | `MotifSketch.Adapters` | Adapters on the Solution Stubs port |
| `MotifSketch.Clipboard` | `Adapters/MotifSketch.Clipboard/` | `classlib` | `MotifSketch.Adapters` | Adapters on the Clipboard port |
| `MotifSketch.Fakes` | `Checks/MotifSketch.Fakes/` | `classlib` | `MotifSketch.Fakes` | Fake driven adapters, for specs |
| `MotifSketch.Specs` | `Checks/MotifSketch.Specs/` | `xunit` | `MotifSketch.Specs` | Specs: the fake on every driving port |

## 3. Project references (dependency rules)

| Project | References |
|---|---|
| `MotifSketch.App` | `MotifSketch.UseCases` |
| `MotifSketch.Domain` | nothing |
| `MotifSketch.UseCases` | `MotifSketch.Domain` |
| `MotifSketch.Projects` | `MotifSketch.UseCases` |
| `MotifSketch.Autosave` | `MotifSketch.UseCases` |
| `MotifSketch.DiagramExports` | `MotifSketch.UseCases` |
| `MotifSketch.SolutionStubs` | `MotifSketch.UseCases` |
| `MotifSketch.Clipboard` | `MotifSketch.UseCases` |
| `MotifSketch.Fakes` | `MotifSketch.UseCases` |
| `MotifSketch.Specs` | `MotifSketch.UseCases`, `MotifSketch.Fakes` |

Rules:

- `MotifSketch.Domain` references nothing.
- `MotifSketch.UseCases` references `MotifSketch.Domain` only.
- Every adapter project references `MotifSketch.UseCases` only. **Adapters never reference each other.**
- The specs project may also reference the fakes it wires up by hand.
- These references are the dependency rule. Don't add others.

## 4. Namespaces

- `MotifSketch.Domain` and `MotifSketch.UseCases` both use the plain `MotifSketch` namespace, so the port interfaces are `MotifSketch.UserTasks` and so on.
- Every other project uses the namespace in section 2. Real and simulated adapters share one namespace, and so do the fakes.
- Folders inside a project don't change its namespace.
- Set `RootNamespace` in each `.csproj` to its namespace, so new files created in an IDE get it.
- Every namespace sits inside `MotifSketch`, so adapters, fakes and specs see the port interfaces without a `using`.

## 5. Ports

All port interfaces live in `MotifSketch.UseCases`, namespace `MotifSketch`, one file each. Interfaces take the plain name of the port, **with no `I` prefix**. Leave them empty: their members are design work still to do.

```
MotifSketch.UseCases/
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
namespace MotifSketch;

public interface UserTasks
{
}
```

`UserQueries.cs`:

```csharp
namespace MotifSketch;

public interface UserQueries
{
}
```

### Projects (driven)

Opens and saves project files

`Ports/Projects.cs`:

```csharp
namespace MotifSketch;

public interface Projects
{
}
```

### Autosave (driven)

Keeps the work in progress and its file handle between visits

`Ports/Autosave.cs`:

```csharp
namespace MotifSketch;

public interface Autosave
{
}
```

### Diagram Exports (driven)

Writes the context diagram and the sketch as pictures

`Ports/DiagramExports.cs`:

```csharp
namespace MotifSketch;

public interface DiagramExports
{
}
```

### Solution Stubs (driven)

Hands the solution stub on to be built; v2 may add a GitHub adapter that inflates it in a repo

`Ports/SolutionStubs.cs`:

```csharp
namespace MotifSketch;

public interface SolutionStubs
{
}
```

### Clipboard (driven)

Takes the snapshot link when the Architect copies it

`Ports/Clipboard.cs`:

```csharp
namespace MotifSketch;

public interface Clipboard
{
}
```

Also add one stub class, `MotifSketchApplication` in `MotifSketch.UseCases` (namespace `MotifSketch`), implementing the driving interfaces (`UserTasks`, `UserQueries`). It is the application itself; its constructor will later take the driven ports.

## 6. Adapters

Kinds: **Real** is the production adapter; **Fake** is a simple in-process stand-in for fast specs; **Simulated** is a higher-fidelity stand-in for people running the whole app locally.

| Port | Adapter | Kind | Technology | Project | Class | Source |
|---|---|---|---|---|---|---|
| User | Web App | Real | Blazor WebAssembly UI | `MotifSketch.App` | - | Context diagram: Architect |
| User | Specs | Fake | xUnit | `MotifSketch.Specs` | - | Added in the sketch |
| Projects | Browser Files | Real | File System Access API, download fallback | `MotifSketch.Projects` | `BrowserFilesProjects` | Context diagram: Project Files |
| Projects | Fake Projects | Fake | - | `MotifSketch.Fakes` (`Projects/`) | `FakeProjects` | Added in the sketch |
| Autosave | IndexedDB | Real | Browser storage | `MotifSketch.Autosave` | `IndexedDBAutosave` | Context diagram: Browser Storage |
| Autosave | Fake Autosave | Fake | - | `MotifSketch.Fakes` (`Autosave/`) | `FakeAutosave` | Added in the sketch |
| Diagram Exports | SVG Files | Real | Browser Files (File System Access API, download fallback) | `MotifSketch.DiagramExports` | `SvgFilesDiagramExports` | Context diagram: Diagram Exports |
| Diagram Exports | Fake Diagram Exports | Fake | - | `MotifSketch.Fakes` (`DiagramExports/`) | `FakeDiagramExports` | Added in the sketch |
| Solution Stubs | Markdown Plan | Real | Browser Files (File System Access API, download fallback) | `MotifSketch.SolutionStubs` | `MarkdownPlanSolutionStubs` | Context diagram: Solution Plans |
| Solution Stubs | Fake Solution Stubs | Fake | - | `MotifSketch.Fakes` (`SolutionStubs/`) | `FakeSolutionStubs` | Added in the sketch |
| Clipboard | Browser | Real | Browser Clipboard API (JS interop) | `MotifSketch.Clipboard` | `BrowserClipboard` | Context diagram: Clipboard |
| Clipboard | Fake Clipboard | Fake | - | `MotifSketch.Fakes` (`Clipboard/`) | `FakeClipboard` | Added in the sketch |

For each driven adapter, add the stub class named above implementing its port's interface, in its project's namespace and in the folder shown. For example:

```csharp
namespace MotifSketch.Adapters;

public sealed class BrowserFilesProjects : Projects
{
}
```

Driving adapters (the app among them) keep their project template's default files. Fakes on driving ports are the specs (section 7).

Adapter notes:

- **Web App** (`MotifSketch.App`): One app for the Architect (full editing) and the Viewer (read-only, opened from a snapshot link). HexaSketch allows only one source per adapter, so the Viewer shows as not placed
- **Specs** (`MotifSketch.Specs`): Checks that drive the application through the User port, using the fake driven adapters. In Motif Sketch this is kind Checks; HexaSketch only knows Fake
- **Browser Files** (`MotifSketch.Projects`): Saved projects: one file per system holding the context diagram and, once started, its ports-and-adapters sketch
- **IndexedDB** (`MotifSketch.Autosave`): Autosaved work in progress and its file handle, so a refresh or closed tab loses nothing and Save still goes to the same file
- **SVG Files** (`MotifSketch.DiagramExports`): The context diagram and the ports-and-adapters sketch as pictures
- **Markdown Plan** (`MotifSketch.SolutionStubs`): The solution stub plan a coding agent builds from
- **Browser** (`MotifSketch.Clipboard`): Where Copy link puts a snapshot link, ready to paste

## 7. Fakes and specs

- `MotifSketch.Fakes` (`Checks/MotifSketch.Fakes/`): every fake driven adapter, with a folder per port (`Projects/`, `Autosave/`, `DiagramExports/`, `SolutionStubs/`, `Clipboard/`). The namespace is flat: `MotifSketch.Fakes`.
- `MotifSketch.Specs` (`Checks/MotifSketch.Specs/`): xUnit. Packages: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `coverlet.collector`, `AwesomeAssertions`. Keep the template's packages and add `AwesomeAssertions`.
  Add one placeholder spec class with a single passing `[Fact]`, so `dotnet test` runs. Specs wire the fake adapters up by hand; there is no composition root yet.
- Use the latest stable package versions. Use AwesomeAssertions (`.Should()` style) in specs.

## 8. Not placed

These context diagram elements were deliberately left off the sketch. Don't build anything for them.

- Viewer (primary actor, Web browser (snapshot link))

## 9. Build this

Instructions for the coding agent:

1. Create `MotifSketch.sln` (classic `.sln` format: `dotnet new sln --format sln`).
2. Create every project in section 2 with its template, in its folder, and add it to the solution in the matching solution folder (`dotnet sln add <project> --solution-folder <folder>`, or `--in-root` for top-level projects). Set each project's `RootNamespace`.
3. Delete template placeholder files (`Class1.cs`, `UnitTest1.cs`), except `Class1.cs` in `MotifSketch.Domain`: keep it, with its namespace changed to `MotifSketch`, to show the namespace choice.
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
- Don't add projects, references or packages the plan doesn't list.
