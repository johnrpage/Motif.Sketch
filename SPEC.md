# Motif Sketch: Spec

Motif Sketch is the first tool in the **Motif** suite. It combines Context Sketch and HexaSketch into one browser app. You sketch a **system context diagram**, turn it into a **ports-and-adapters sketch**, and export both as pictures, plus a **solution plan** that a coding agent can build stubs from. It is a Blazor WebAssembly app hosted on GitHub Pages: no server, no accounts, and your work stays in your own files and browser.

The two desktop tools are the reference implementations. Their DESIGN.md files describe their diagram scheme, layout rules and plan opinions. Unless this spec says otherwise, Motif Sketch keeps their behaviour. `decisions.md` records why each choice below was made. `Motif Sketch - Context Diagram.contextsketch.json` and `Motif Sketch.hexasketch.json` are Motif Sketch's own design, drawn in the old tools.

**Workflow request:** use plan mode. Propose the architecture and a phased plan, resolve section 10, and confirm before writing code. Section 9 (look and feel) has been designed separately; follow it and don't invent your own.

## 1. Goals and non-goals

**Goals**
- Go from nothing to a context diagram in under a minute, then to a ports-and-adapters sketch, in one tool and one file.
- Layout is automatic in both views. The user adds, names and moves things, and never lays anything out by hand.
- Projects are readable, diff-friendly JSON files.
- Nothing is lost on refresh: work autosaves to the browser.
- Export SVG pictures of both views and a Markdown solution plan.
- Share a read-only view with a link, with no server involved.
- Runs on any modern browser. Edge and Chrome also get save-in-place.

**Non-goals (v1)**
- Accounts, a backend, real-time collaboration, telemetry.
- GitHub integration: no sign-in, repos, gists, or agent hand-off (see "Later" in decisions.md).
- Importing old `.contextsketch.json` / `.hexasketch.json` files.
- PNG/PDF export, plugins, configurable plan templates.
- Container- or component-level diagrams.

## 2. Tech constraints

- **C# on .NET 10**. **Blazor WebAssembly, standalone** (no ASP.NET host), deployable as static files.
- **Hosting: GitHub Pages**, project site at `https://<user>.github.io/Motif.Sketch/`. A GitHub Actions workflow builds, publishes and deploys on push to `main`. `<base href>` is set for the sub-path, and a `.nojekyll` file is included. The app uses one page: the views are app state, not routes, so deep links never 404 on Pages.
- **JSON via `System.Text.Json`**: camelCase names and enum strings, indented, with stable ordering.
- **Browser APIs through small, hand-written JS interop modules**, one per adapter: File System Access, file download/upload, IndexedDB, Clipboard. Each module is as thin as possible, and all logic stays in C#.
- Minimal NuGet dependencies, each one justified in the plan. No JS frameworks and no npm build step.
- **Tests: xUnit with AwesomeAssertions.** The application is driven through its User port with fake driven adapters. No browser is needed for the specs.
- Nullable reference types on. Builds with no warnings.

## 3. Architecture

Motif Sketch is built as ports and adapters, exactly as `Motif Sketch.hexasketch.json` shows:

| Port | Side | Real adapter | Fake / checks |
|---|---|---|---|
| User | driving | **Web App** (the Blazor UI, Architect and Viewer modes) | **Specs** (xUnit, kind Checks) |
| Projects | driven | **Browser Files** (File System Access API, download fallback) | Fake Projects |
| Autosave | driven | **IndexedDB** | Fake Autosave |
| Diagram Exports | driven | **SVG Files** (File System Access API, download fallback) | Fake Diagram Exports |
| Solution Stubs | driven | **Markdown Plan** (File System Access API, download fallback) | Fake Solution Stubs |
| Clipboard | driven | **Browser** (Clipboard API) | Fake Clipboard |

- The project layout and naming follow the plan Motif Sketch itself would export: roles as folders (`Motif.Sketch/`, `Adapters/`, `Checks/`), one project per driven port, and port interfaces named plainly with no `I` prefix. The root namespace is **`Motif.Sketch`**, keeping the dot (`Motif.Sketch.Domain`, `Motif.Sketch.UseCases`, `Motif.Sketch.Adapters`, ...).
- Domain references nothing. UseCases references Domain only. Every adapter references UseCases only, and adapters never reference each other.
- The composition root is in the Web App and is the only place that knows which adapters are used. It picks the save-in-place or the download behaviour inside the file adapters by feature detection.
- **Pure core logic**: validation, both layouts, SVG rendering, plan generation, snapshot-link encoding and the bundled example all live in Domain or UseCases and need no adapter. SVG text and plan Markdown are generated there; the export adapters only write them.
- **The example project** is an embedded resource in the core. The Specs use it as a fixture.

## 4. Domain model

### 4.1 Project

One project per system, saved as **`.motifsketch.json`**:

```json
{
  "schemaVersion": 1,
  "title": "Motif Sketch",
  "contextDiagram": { "elements": [ ... ], "relationships": [ ... ] },
  "sketch": { "ports": [ ... ] }
}
```

- `contextDiagram` is always present. `sketch` is **optional**: it's absent until the user starts the ports-and-adapters view, so a project with only a context diagram is valid.
- The context diagram and the sketch live in the same file and refer to each other by id. Nothing is copied, so nothing can drift (contrast HexaSketch's embedded copy).
- Indented output with stable ordering. Save, then load, gives an identical file.

### 4.2 Context diagram

As in Context Sketch:
- **Element** fields: `id`, `type` (`system` | `primaryActor` | `secondaryActor` | `externalSystem` | `storage`), `name`, optional `description` and `technology`, plus `x`, `y` and `pinned`.
- **Relationship** fields: `id`, `from`, `to`, `label` and `direction`.
- **Rules:**
  - There is exactly one system, created with the project. It can't be deleted.
  - Every other element has **exactly one** relationship, and the system is its other end. That relationship is created when the element is added.
  - There are no self-relationships.
  - Deleting an element deletes its relationship.
- **Arrows mean "who initiates"**: an arrow points from the side that starts the interaction. `twoWay` is for the rare case where both sides start one (e.g. webhooks). The app's help says this.

### 4.3 Sketch

As in HexaSketch, with these changes:
- **Port** fields: `id`, `name`, optional `description`, `side` (`driving` | `driven`) and `isUser`.
  - The User port always exists in a sketch and can't be deleted.
  - Every driving port starts with a **Specs** adapter of kind `checks`.
- **Adapter** fields: `id`, `name`, optional `technology` and `description`, `kind` and `sources`.
  - `kind` is `real`, `fake`, `simulated` or **`checks`**. `checks` is allowed on driving ports only: it's a test driver that drives the application, not a stand-in for something the application calls.
  - **`sources: [elementId, ...]`**: the context diagram elements this adapter stands for. It can be empty (a hand-made adapter) or **hold several**, e.g. one Web App serving two actors. An element can be a source of at most one adapter.
- Starting the sketch creates the User port, and **every primary actor becomes a source of one Real adapter on it**, named after the actor (the same as HexaSketch). The user can then merge those adapters.
- Every other context diagram element waits in an **unplaced list** until the user places it.

### 4.4 Validation

Loading validates the file and reports every problem in a clear message, rather than crashing:
- unknown schema version or element type
- duplicate ids, or dangling references in relationships or `sources`
- not exactly one system, or a relationship rule broken
- an element used as a source by more than one adapter
- `checks` on a driven port, or a missing User port

An invalid file leaves the current project untouched.

## 5. Behaviour

### 5.1 Context diagram view

Context Sketch's behaviour, unchanged:
- Add each element type with one click. The element gets a default name, is auto-placed and is selected.
- Edit the selected item in a properties panel. Double-click to rename, and press Delete to delete.
- Drag an element to pin it. **Auto-layout** moves only unpinned elements, and **Auto-layout all** releases every pin first.
- Pan and zoom.
- The layout zones and spacing rules are Context Sketch's (DESIGN.md §3): primary actors on the left; on the right, secondary actors, then external systems, then storage, top to bottom. It's deterministic and never overlaps.

### 5.2 Ports-and-adapters view

HexaSketch's behaviour (its DESIGN.md "Sketching"), plus:
- **Start sketch** creates the sketch from the context diagram (§4.3). Once a sketch exists, the user can switch freely between the two views.
- **Placing an item:**
  - Drag it from the unplaced list onto a port to make a new adapter there.
  - Drag it onto the hexagon to make a new port named after it, driving or driven depending on which half it's dropped in.
  - Drag it onto an **existing adapter** to merge it in, adding it to that adapter's sources.
- Removing an adapter, or removing a source from one, returns that item to the unplaced list.
- **Default names:**
  - A fake is named **"Fake ⟨Port⟩"**.
  - The checks adapter on a driving port is named **"Specs"**, technology "xUnit".
  - A real adapter is named after the context diagram item until renamed. The help suggests naming it after its technology, which keeps class names clean.
- Layout is automatic. The user can reorder ports and adapters but not position them freely, as in HexaSketch.

### 5.3 Keeping the views in step

The views share one project, so a change to the context diagram shows up in the sketch straight away:

| Change to the context diagram | Effect on the sketch |
|---|---|
| Rename an element | An adapter whose name is still its source's name follows the rename. A renamed adapter keeps its own name. |
| Edit description or technology | Nothing: adapters have their own fields. |
| Add an element | It appears in the unplaced list. |
| Delete an element | It's removed from its adapter's sources. The adapter stays, flagged "source deleted" until the user edits, re-sources or deletes it. Plan export lists flagged adapters as a warning. |
| Change an element's type | Its placement is kept. |

Each of these is part of the same undo step as the context diagram edit that caused it.

### 5.4 Undo and redo

Every edit in either view is one undo step: Ctrl+Z / Ctrl+Y. There is one history per open project, covering both views.

### 5.5 Files

- **New**, **Open**, **Save** and **Save As**, on `.motifsketch.json` files.
- In **Edge and Chrome**, File System Access is used. Save writes back to the file that was opened or last saved, and Save As picks a new file.
- In **other browsers**, Open uses a file picker and Save downloads a copy.
- **Unsaved changes** are tracked. The page title shows a marker. New and Open ask before discarding work, and leaving the page with unsaved file changes warns (`beforeunload`).

### 5.6 Autosave

- After every edit (debounced), the current project is written to **IndexedDB**, together with its **file handle** when there is one.
- On the next visit, the work is restored without asking. If a handle was kept, the first Save asks the browser for permission to write to that file again, then writes to it.
- Autosave is a safety net, not a substitute for saving: "unsaved changes" still means not yet saved to a file.
- There is only ever **one** autosaved project, the current one.
- **One tab owns autosave.** If the app is opened in a second tab, that tab is read-only and says the app is open in another tab. It offers **Use this tab instead**, which hands ownership over. The first tab then goes read-only, after its last autosave is written. Tabs coordinate through the browser (Web Locks or `BroadcastChannel`, planner's choice). Viewer-mode tabs (§5.8) never touch autosave, so they don't take part.

### 5.7 Exports

- **Export context diagram** and **Export sketch** each write a standalone SVG of that view, cropped to the content.
  - The on-screen-only parts (selection highlight, centre divider) are left out.
  - The SVG and the screen are drawn from the same style definitions: the same shapes, layout and text. Exports always use the light palette; on screen, dark mode swaps in the dark twin of that palette.
- **Export plan** writes the Markdown solution plan to the Solution Stubs port. It follows HexaSketch's plan opinions with these changes:
  - **Dotted names are kept as the root**: `Motif.Sketch.Domain`, not `MotifSketch.Domain`.
  - **Two-letter acronyms stay in capitals** in code names (`IndexedDB`, `UI`), which overrides the .NET guidelines.
  - The User port's runnable Real adapter is `<Root>.App`, or one project per adapter if there are several. Each adapter lists the context diagram items it stands for.
  - A `checks` adapter on a driving port becomes `<Root>.Specs` (xUnit and AwesomeAssertions).
  - A fake class is `Fake<Port>`. A real adapter's class is `<AdapterName><Port>`, unless the adapter name only repeats the port name, in which case the technology is used instead.
- Both exports warn, but still allow it, when items are unplaced. The plan lists them under "Not placed". Plan problems block the plan export: name clashes, unusable names, or reserved names.
- Exports use the same File System Access / download behaviour as projects.

### 5.8 Sharing (snapshot links)

- **Copy link** compresses the whole project into the URL fragment, `https://<user>.github.io/Motif.Sketch/#v1.<base64url>`, and puts it on the Clipboard port.
- Everything after `#` stays in the browser and is never sent to a server.
- The link is a **snapshot**: later edits need a new link.
- If the link would be very long, the app says so but still copies it. The threshold is the planner's choice. Most chat and email tools handle links up to several KB.
- **Opening a snapshot link** starts the app in **Viewer mode**:
  - The project is read-only, and both views are viewable.
  - Viewer mode **never touches autosave**, so it can't overwrite the visitor's own work.
  - The viewer can **Make a copy to edit**, which turns it into ordinary work and replaces their autosave after asking.
- Opening a malformed or old-version link shows a friendly error.

### 5.9 Example

- **Open example** loads the bundled project, which is Motif Sketch's own design (both views), as an ordinary unsaved project, after asking if there's unsaved work.
- Until the real `.motifsketch.json` exists, the example is built from the two old files in this folder.

## 6. Diagram visuals

These are carried over unchanged, because they're part of the suite's identity:
- **Context diagram:** Context Sketch's element shapes and its slate palette, with the ink-outlined system and the orange on-screen selection. Technology is shown as faded text under the name, and long labels wrap the same way.
- **Sketch:** HexaSketch's pointy-topped hexagon:
  - Domain is a circle, with Tasks and Queries on the left.
  - Ports are circles on stalks, and the User port is heavier.
  - Real adapters are solid slate. Fake and simulated adapters are white and dashed.
  - The User area is red.
- **Checks adapters** are pale slate boxes with a solid outline, between solid Real and white dashed Fake (decision 32).
- Diagram text uses IBM Plex Sans, whose width table the core uses to wrap labels (decision 34).

## 7. Quality bar

- **Specs** drive the application through the User port with the fakes. Together they cover:
  - every rule in §4
  - save/load round-trip
  - both layouts: deterministic, no overlaps, zones respected, pins respected
  - SVG output (well-formed, contains the expected elements)
  - the plan, using the example project as a golden file
  - snapshot links (encode then decode gives the same project; a bad link gives an error)
  - Viewer mode never writing autosave
- The JS interop modules are thin enough to check by hand. Automated browser tests are optional (section 10).
- `dotnet build` is clean and `dotnet test` passes. The GitHub Actions workflow runs the tests before deploying.

## 8. Acceptance criteria

- [ ] I can sketch a context diagram as fast as in Context Sketch, with the same layout.
- [ ] Start sketch turns it into a ports-and-adapters sketch. I can place, merge, move and remove items, and switch freely between the views.
- [ ] Specs is there on the User port from the start. Fakes are named "Fake ⟨Port⟩".
- [ ] Save, close the tab, reopen: in Edge/Chrome, Save still goes to the same file. In Firefox, Save downloads a copy.
- [ ] A refresh mid-edit loses nothing.
- [ ] Both SVG exports open in a browser and match the screen in light mode.
- [ ] The plan uses `Motif.Sketch.*` names, and following it gives a solution with the same shape as this app.
- [ ] A snapshot link opens read-only in another browser, and Make a copy turns it into my own work.
- [ ] Open example shows Motif Sketch's own design.
- [ ] Invalid files and links give friendly errors and never lose the current work.
- [ ] Pushing to `main` deploys to GitHub Pages.

## 9. UI and look and feel

This spec fixes what the UI must offer, not how it looks:
- the two views, and the means of switching between them
- the commands in §5
- a properties panel
- the unplaced list in the sketch view
- pan and zoom
- keyboard shortcuts (Delete, rename, undo/redo, save)
- a clear Viewer-mode indication
- friendly error and confirmation messages

The **look and feel** has had its design pass. The decisions are 26–39 in `decisions.md`, and the mockups are `docs/mockups/layout.html` (layout) and `docs/mockups/v2.html` (look):
- **Layout and chrome:** three layouts (Context, Split, Sketch), with the hidden view shrunk to a live thumbnail strip, and a three-way switch. A slim app bar holds the project commands, and each pane has its own tool strip. Properties and the unplaced list are floating cards.
- **Identity:** IBM Plex Sans, self-hosted. The chrome is slate plus orange only. The logo is a Motif suite mark (stepped squares) plus the tool name. Light and dark follow the system, with an override. The diagrams have a dark twin on screen, and exports are always light.
- **Devices:** desktop and tablet edit, with touch. Phones are read-only.
- **Checks adapters:** pale slate (§6).
- **First-run:** a blank project with the system ready to name, plus a dismissible welcome card.
- **Viewer mode:** editing controls hidden; Make a copy to edit as the one primary action.

The planner follows these. The styling in `v2.html` is a first pass, so exact spacing and shades may still change. A plain placeholder UI is fine for the early phases.

## 10. Decisions for the planner

Please decide and justify briefly in the plan:
1. Canvas rendering: SVG in Razor components vs `<canvas>`. Is the screen drawn by the same renderer as the export?
2. Interaction plumbing: drag and drop, pan/zoom and keyboard, and how much needs JS interop.
3. Snapshot link encoding: compression (.NET `DeflateStream` in WASM vs the browser's `CompressionStream`), base64url, the version prefix, and the length warning threshold.
4. Undo/redo mechanism across both views.
5. App startup time and download size (trimming, AOT or not), and whether a loading screen is needed.
6. Whether to add automated browser tests (e.g. Playwright) for the JS interop adapters, or check them by hand.
7. Phasing: a working slice early, deployed to Pages from the first phase. For example: core model, JSON and specs → context diagram view → files and autosave → sketch view → exports and plan → sharing → example → look-and-feel pass applied.

## 11. Open questions

None. Q1 (context diagram changes) and Q2 (several tabs) are settled in §5.3 and §5.6.
