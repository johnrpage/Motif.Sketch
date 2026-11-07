# Motif Sketch: decisions so far

Motif Sketch is the first tool in the Motif suite. It combines Context Sketch and HexaSketch into one web app.

| # | Topic | Decision |
|---|---|---|
| 1 | Name | Shown to people as "Motif Sketch". The repo, solution and namespaces use `Motif.Sketch` (e.g. `Motif.Sketch.Domain`). |
| 2 | Files | One project file per system: the context diagram, plus its ports-and-adapters sketch once you start one. The sketch part is optional. |
| 3 | Import | No import of old `.contextsketch.json` / `.hexasketch.json` files. |
| 4 | Autosave | The current work autosaves to browser storage and comes back on the next visit. Saving to a file is still something you do yourself. |
| 5 | Saving | Edge and Chrome save back to the same file (File System Access API). Other browsers download a copy. |
| 6 | GitHub | Hosting on GitHub Pages only. GitHub sign-in or projects stored in a repo: maybe later, not v1. |
| 7 | Actors | Architect and Viewer (opens a shared diagram read-only). No Coding Agent in the diagram. |
| 8 | Exports | Split into Diagram Exports (SVG) and Solution Plans (Markdown). In the sketch, the plan is the `SolutionStubs` port, and the Markdown plan is its v1 adapter. |
| 9 | Arrows | An arrow points from whoever starts the interaction. Two-way is kept for the rare case where both sides start one (e.g. a webhook callback). The tool's help says this. |
| 10 | Sharing | Snapshot links: the project is compressed into the URL fragment (after `#`), so it's never sent to a server and nothing is stored. Opening the link shows a read-only view. A later edit needs a new link. |
| 11 | File extension | `.motifsketch.json`: one per tool, the same pattern as `.contextsketch.json`. |
| 12 | Browser storage | IndexedDB, not localStorage. It keeps the autosaved project and its File System Access handle, so after a refresh Save still goes to the same file (the browser asks once to allow it again). |
| 13 | Example | "Open example" loads a read-only copy of Motif Sketch's own project (context diagram and sketch), shipped with the app. It's also a test fixture. It gets built once the ports-and-adapters sketch exists. It's part of the system, so the context diagram doesn't show it. |
| 14 | One app | Architect and Viewer use one Blazor app, the "Web App" adapter on the User port. The Viewer gets a read-only mode. |
| 15 | Merged adapters | An adapter can stand for several context diagram items on any port (`sources: [...]`, not `source`). Drop an item onto an adapter to merge it in. One adapter per item still works, e.g. one app per actor. The plan says which items each adapter covers. |
| 16 | Dotted names | The plan export keeps a dotted system name as the root (`Motif.Sketch.Domain`), unlike HexaSketch, which produces `MotifSketch`. |
| 17 | Specs | The Specs adapter is drawn in the sketch. Motif Sketch adds one automatically to every driving port, just as it creates the User port. |
| 18 | Fake names | A fake is named after its port by default: "Fake Projects", which matches the generated class `FakeProjects`. |
| 19 | Real adapter names | Real adapters are named after their technology, not the context diagram item, so class names don't stutter: `BrowserFilesProjects`, `IndexedDBAutosave`, `SvgFilesDiagramExports`, `MarkdownPlanSolutionStubs`. Each adapter keeps its context diagram item as its source. |
| 20 | Acronyms | Two-letter acronyms stay in capitals in code names (`IndexedDB`, `UI`), which overrides the .NET guidelines' `Db`. |
| 21 | Checks kind | A fourth adapter kind, **Checks**, for driving ports only: a test driver that drives the application and checks what it does. It isn't a Fake, which stands in for something the application calls. The User port's Checks adapter is named "Specs" (technology xUnit) and becomes `Motif.Sketch.Specs`. Kinds are now Real, Fake, Simulated, Checks. |
| 22 | Clipboard | Copy link goes through a driven **Clipboard** port: real adapter "Browser" (Clipboard API, JS interop) → `BrowserClipboard`, plus Fake Clipboard so specs can check the copied link. The context diagram shows Clipboard as an external system. |
| 23 | Example source | The example project is compiled into the application core as an embedded resource. Reading it involves no outside I/O, so it needs no port, and specs can open it directly. |
| 24 | Context diagram changes | When the context diagram changes after the sketch exists: an adapter still carrying its source's name follows a rename, and a renamed one keeps its own name. Description and technology edits leave adapters alone. Added elements go to the unplaced list. Deleting an element removes it from its adapter's sources and flags the adapter, which stays. A type change keeps the placement. (SPEC §5.3) |
| 25 | Several tabs | One tab owns autosave. A second tab is read-only and offers "Use this tab instead", which hands ownership over (the first tab goes read-only). Viewer-mode tabs never take part. (SPEC §5.6) |

## Later (not v1)

- A GitHub adapter on `SolutionStubs` that creates a repo, commits the plan, and hands it to a coding agent (e.g. by opening an issue) to inflate the stub solution. It needs GitHub sign-in, which a static Pages app can't do with standard OAuth (a token or a small backend instead). The v2 context diagram would add GitHub as an external system, and possibly the agent as a secondary actor.
- Short share links that update, hosted in a GitHub Gist (needs the same GitHub sign-in).

## Still open

Nothing. The spec (SPEC.md) has no open questions either.
