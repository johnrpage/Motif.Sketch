# Motif Sketch: build plan

## Context

Motif Sketch is fully specified: SPEC.md, decisions 1–39 and the mockups in `docs/mockups/`. It has no code yet. The spec asks for an architecture and a phased plan that resolves the seven planner decisions in SPEC §10, confirmed before any code is written.

There are two good starting points:
- **The solution plan HexaSketch exported from Motif Sketch's own sketch** (`docs/Motif Sketch - plan.md`). It gives the project layout. SPEC §3 says the solution follows "the plan Motif Sketch itself would export", so we use that layout, corrected wherever Motif Sketch's own exporter will differ.
- **The two reference apps' core libraries, which are plain C# with no UI.** They can be ported almost verbatim:
  - Context Sketch (`..\Context Tool\src\ContextSketch.Core`): the model, `DiagramValidator`, `DiagramJson`, `AutoLayout` with `LayoutOptions`, the geometry types (`Primitives`, `ElementShapes`, `EdgeGeometry`), `TextWrap`, `DiagramStyle` and `SvgExporter`, plus their xUnit tests.
  - HexaSketch (`..\HexaSketch\src`, the canonical copy with the GitHub remote): from Domain, `Sketch`, `Port` and `Adapter`; from Application, `SketchSession` (snapshot undo), `Diagram/DiagramLayout` with `DiagramView`, `Diagram/SvgDiagram`, and `Plan/SolutionPlanner`, `CodeName` and `PlanMarkdown`; about 55 specs.
  - Only the two WPF shells need rewriting, as Blazor.

**Reusing the old code: review, simplify, fit.** Ported code is a starting point, not something to copy unexamined. For every file brought over from Context Sketch or HexaSketch:
- **Review it** as if it were new code: read it, understand it, and drop anything Motif Sketch doesn't need (for example HexaSketch's embedded context-diagram copy and its legacy-format import, ContextSketch's MVVM coupling, `IDialogService`).
- **Simplify where you can:** merge duplicated logic between the two apps (two text-width estimates, two SVG writers, two validators, two JSON DTO sets), remove dead options, and prefer fewer, clearer types.
- **Fit it into this architecture,** not the old one: put it in the right project (Domain for rules, UseCases for application logic and ports), name it to Motif's conventions (no `I` prefix, two-letter acronyms in capitals), keep the dependency rule, and reach it through the User port so the specs can drive it.
- **Port the tests with it,** rewritten as specs through the User port where they test behaviour, and check that each one still earns its place.

## Solution layout

This is the HexaSketch export with the corrections listed after it. The namespaces follow the export, with Domain and UseCases sharing the root `Motif.Sketch` (chosen in the planning session). SPEC §3's wording gets fixed to say so.

```
Motif.Sketch.sln                      (classic .sln)
  Motif.Sketch.App/                   blazorwasm, standalone: Web App adapter + composition root
  Motif.Sketch/
    Motif.Sketch.Domain/              ns Motif.Sketch
    Motif.Sketch.UseCases/            ns Motif.Sketch: ports, application, layouts, SVG, plan, JSON, snapshot codec, example
  Adapters/
    Motif.Sketch.Projects/            BrowserFilesProjects        ns Motif.Sketch.Adapters
    Motif.Sketch.Autosave/            IndexedDBAutosave
    Motif.Sketch.DiagramExports/      SvgFilesDiagramExports
    Motif.Sketch.SolutionStubs/       MarkdownPlanSolutionStubs
    Motif.Sketch.Clipboard/           BrowserClipboard
  Checks/
    Motif.Sketch.Fakes/               FakeProjects, FakeAutosave, FakeDiagramExports, FakeSolutionStubs, FakeClipboard
    Motif.Sketch.Specs/               xUnit + AwesomeAssertions
```

Root files:
- the export's `.editorconfig`, which turns off the `I` prefix rule for interfaces
- `Directory.Build.props`: `net10.0`, Nullable, TreatWarningsAsErrors, ImplicitUsings
- `.github/workflows/pages.yml`

The references follow the export's rules exactly: Domain references nothing; UseCases references Domain only; each adapter references UseCases only; Specs references UseCases and Fakes; App references UseCases plus the adapters, because it is the composition root.

**Where we depart from the HexaSketch export** (each is also a requirement on Motif Sketch's own plan exporter):

| HexaSketch export | Motif Sketch | Why |
|---|---|---|
| `MotifSketch.*` | `Motif.Sketch.*`, core folder `Motif.Sketch/` | Decision 16 |
| `IndexedDb`-style casing (n/a here) | Two-letter acronyms stay in capitals | Decision 20 |
| App template `web` | `blazorwasm` | Exporter should map "Blazor WebAssembly" to `blazorwasm` |
| Adapters on `classlib` | Razor SDK (`razorclasslib` with its sample removed) | Each adapter ships its JS module as a static web asset (`_content/<project>/x.js`) |
| No extra packages | `Microsoft.JSInterop` in the 5 real adapters | Needed for `IJSRuntime`; this is the one justified package |
| Specs is a Fake | Specs is kind Checks (same project) | Decision 21 |
| Viewer under "Not placed" | Viewer is a second source of Web App | Decision 15 |
| Stubs only, no composition root | Real app; App is the composition root | We're building it |

## Core design (Domain and UseCases)

**Domain**
- `Project` holds the title, a `ContextDiagram`, and an optional `Sketch` that refers to elements by id (no embedded copy).
- `ContextDiagram`, `Element` and `Relationship` are ported from ContextSketch.Core's `Model/Diagram`.
- `Sketch`, `Port` and `Adapter` are ported from HexaSketch.Domain, with these changes:
  - `SourceElementId` becomes `Sources: IReadOnlyList<string>`.
  - `AdapterKind` gains `Checks`.
  - `Adapter` gains a `SourceDeleted` flag.
  - The rules, including §5.3's sync effects, live on `Project`.
- `ProjectValidator` merges `DiagramValidator` and HexaSketch's `Problems()` with the §4.4 checks.

**UseCases**
- **Ports** (plain names, no `I`):
  - Driving: `UserTasks` and `UserQueries`.
  - Driven: `Projects`, `Autosave`, `DiagramExports`, `SolutionStubs`, `Clipboard`.
- **`MotifSketchApplication`** implements both driving ports and is extended from HexaSketch's `SketchSession`. It holds the open project, the undo history, the dirty flag and the mode (Architect or Viewer); the Viewer mode never calls `Autosave`.
- **`ProjectJson`** is the file format of §4.1: System.Text.Json with camelCase, enums as strings, indented, and DTOs fixing property order (the ContextSketch `DiagramFile` pattern). Adapters only move strings.
- **`ContextLayout`** is `AutoLayout` ported; **`SketchLayout`** is `DiagramLayout` ported.
  - Both produce plain view records, `ContextView` and `SketchView` (`DiagramView` generalised).
  - **Text widths come from a `PlexMetrics` table** (advance widths for IBM Plex Sans 400 and 600, generated once from the font's `hmtx` table and committed as C#). It replaces both old 0.55/0.56 estimates and `TextWrap`'s character-count heuristics.
- **`SvgWriter`** is `SvgExporter` and `SvgDiagram` ported: `System.Xml.Linq`, the light palette, cropped, with no on-screen parts.
- **`DiagramStyle`** is the single token table (shapes, strokes, font sizes, radii, line height, light palette and dark twin). The SVG writer uses its light values; the App generates CSS custom properties from it.
- **`SolutionPlanner`, `CodeName` and `PlanMarkdown`** are ported, with the changes above, plus:
  - a Checks adapter becomes `<Root>.Specs`
  - flagged "source deleted" adapters and unplaced items produce warnings
  - name problems block the export
- **`SnapshotLink`** encodes and decodes `#v1.<base64url>` (§10.3).
- **`Example`** reads an embedded `example.motifsketch.json`.

## Decisions for SPEC §10

1. **Rendering: SVG in Razor components, drawn from the same view records as the export.** The App renders `ContextView` and `SketchView` with components, not by injecting `SvgWriter`'s string. The screen needs hit-testing, the orange selection, dark-mode CSS variables and live thumbnails (the same component, scaled and inert). The screen and the export share layout, geometry, text lines and `DiagramStyle` tokens. They differ only in screen-only parts and the dark palette (decision 33). A spec checks that both list the same elements in the same positions.
2. **Interaction: Blazor pointer events, plus one small `canvas.js` in the App.**
   - In C#: pointer down, move and up for select, drag-to-pin, pan, and dragging items from the Not placed card. Touch works too, because HTML5 drag-and-drop doesn't. Pinch is computed from two pointers.
   - JS is needed only for:
     - `setPointerCapture`
     - a non-passive `wheel` listener for zoom
     - converting screen coordinates to SVG coordinates (`getScreenCTM`)
     - a document-level `keydown` that forwards Delete, F2, Ctrl+Z, Ctrl+Y, Ctrl+S, Alt+1/2/3 and Esc
     - `beforeunload`
   - Pan and zoom are a viewBox transform held in C#.
3. **Snapshot links: .NET `DeflateStream` in the core.**
   - The core minifies the JSON (no indentation), deflates it (raw deflate) and encodes it as base64url with a `v1.` prefix.
   - It stays in pure C#, so the specs can test encode-then-decode and bad links without a browser. `System.IO.Compression` works in browser WASM.
   - If a link is over **8,000 characters**, the app warns but still copies it.
4. **Undo and redo: whole-project snapshots** (HexaSketch's `SketchSession` approach). Every user command clones `Project` once; the §5.3 sync runs inside the same command, so it is one step. Projects are small, so cloning is cheap; the history is capped at 200 steps and covers both views.
5. **Startup and size: trimmed, no AOT, a static splash screen.**
   - Publish uses the default trimming plus `InvariantGlobalization=true` to drop ICU. AOT roughly doubles the download for little gain in an app this light.
   - `index.html` shows the Motif mark and "Loading…" until Blazor boots.
   - Download size is measured in phase 0, aiming for about 3 MB compressed (GitHub Pages gzips; it can't serve Blazor's prebuilt `.br` files).
6. **Browser tests: none in v1.** The five JS modules stay thin, and `docs/manual-checks.md` holds a checklist to run before each deploy (Chrome and Firefox, save-in-place, autosave restore, two tabs, link, clipboard). Playwright can be added if a regression gets through.
7. **Phasing**, below. Each phase deploys to Pages.

**Browser file handles across adapters.** A File System Access handle comes from Projects but has to be stored by Autosave, and adapters never reference each other. UseCases therefore passes only an opaque `FileHandleId` (a string). On the JS side, `globalThis.motifFileHandles` is a `Map` from id to handle, documented as the one shared browser contract. Projects' module adds handles to it. Autosave's module stores the handle in IndexedDB next to the JSON and puts it back in the map on restore.

The small "save a file" helper is copied into the Projects, DiagramExports and SolutionStubs modules (about 30 lines each), so the three adapters stay independent.

**Tab ownership:** `navigator.locks` (Web Locks) with `ifAvailable` decides who owns autosave. A `BroadcastChannel` carries "Use this tab instead", and the old tab flushes its last save before it releases the lock.

## Phases

0. **Scaffold and deploy.**
   - Create the solution above with stub ports, stub adapters, the placeholder spec, the `.editorconfig` and the props file.
   - Add the Pages workflow: `dotnet test`, publish, set `<base href>` to `/Motif.Sketch/`, add `.nojekyll`, deploy with `actions/deploy-pages`. The page shows a placeholder.
   - Measure the download size.
1. **Core model, JSON and specs.**
   - Port the Domain and the validator, `ProjectJson`, and the `Project` rules including §5.3.
   - Generate `PlexMetrics`.
   - Convert the two old diagram files once into `example.motifsketch.json` and embed it (a one-off spec-time converter, then deleted).
   - Port the ContextSketch and HexaSketch rule specs, adapted.
2. **Context diagram view.**
   - Port `ContextLayout`, add text wrapping on `PlexMetrics`, and build the App shell on the decided layout: app bar, tool strips, floating properties card with corner placement, three-way switch (a single view until phase 4).
   - Add element, rename, delete, drag-to-pin, Auto-layout, pan and zoom, undo and redo.
3. **Files and autosave.** Build the Projects and Autosave adapters, the handle map, the dirty marker and title, `beforeunload`, permission to write again, tab ownership and the read-only second tab.
4. **Sketch view.**
   - Port `SketchLayout` and the sketch operations; add multiple sources, merging, Checks, Start sketch.
   - Build the Not placed card and badge, pointer drag-and-drop, reordering, the "source deleted" flag.
   - Add the Split and Sketch layouts with live thumbnails.
5. **Exports and plan.** Port `SvgWriter` and the planner with Motif's naming rules, warnings and blocking problems. Add the DiagramExports and SolutionStubs adapters. **Golden-file spec:** the plan exported from the example matches a checked-in `Motif.Sketch.plan.md`, which is the HexaSketch export corrected as in the table above.
6. **Sharing and Viewer.** Add `SnapshotLink` and the Clipboard adapter, Viewer mode as in decision 31, Make a copy to edit, and friendly errors for bad links.
7. **Example and the look applied.**
   - Add Open example and the first-run welcome card.
   - Self-host IBM Plex (woff2, OFL) and apply v2's styling and the dark twin.
   - Add the theme override and the stepped-squares mark and favicon.
   - Add tablet touch (pinch, long-press) and read-only below 600px.
   - Final pass against the SPEC §8 acceptance checklist.

## Docs to update alongside

- **SPEC §3:** say that the core namespace is `Motif.Sketch`, shared by Domain and UseCases. Note the Razor SDK and JSInterop for adapters.
- **decisions.md:** add the §10 answers and the handle map.
- **README:** add a build and run section once phase 0 exists.

## Verification

- `dotnet build` with no warnings and `dotnet test` green in every phase. The specs drive `MotifSketchApplication` through `UserTasks` and `UserQueries` with the fakes, and cover:
  - the §4 rules
  - the round trip (save, load, identical output)
  - layout determinism, no overlaps, zones and pins
  - well-formed SVG with the expected elements
  - the plan golden file
  - snapshot link round trip and bad-link errors
  - Viewer mode never writing autosave
- Each phase is deployed to `https://<user>.github.io/Motif.Sketch/` by the workflow and checked in the browser pane against `docs/mockups/v2.html` and `docs/manual-checks.md`.
- The SPEC §8 acceptance checklist is ticked off at the end of phase 7.
