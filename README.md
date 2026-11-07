# Motif Sketch

The first tool in the **Motif** suite. You sketch a system context diagram, turn it into a ports-and-adapters sketch, and export both as SVG pictures, plus a Markdown solution plan a coding agent can build stubs from. It's a Blazor WebAssembly app for GitHub Pages: no server and no accounts. Your work stays in your own files and browser.

**Try it: [johnrpage.github.io/Motif.Sketch](https://johnrpage.github.io/Motif.Sketch/)**. File → Open example shows Motif Sketch's own design.

It combines and replaces two desktop tools: [Context Sketch](https://github.com/johnrpage/context-tool) and [HexaSketch](https://github.com/johnrpage/HexaSketch).

## Design

### Context diagram

![Motif Sketch context diagram](docs/context-diagram.svg)

### Ports and adapters

![Motif Sketch ports-and-adapters sketch](docs/ports-and-adapters.svg)

These pictures are rendered from the two diagram files below, by Context Sketch's and HexaSketch's own SVG exporters.

## Status

**Built.** All of [docs/build-plan.md](docs/build-plan.md) is in: both views, files and autosave, exports and the plan, snapshot links and Viewer mode, the example, and the v2 look with its dark twin. The answers to SPEC §10 are decisions 40–49.

### Still to do

1. **Run [docs/manual-checks.md](docs/manual-checks.md)** on the live site in Chrome or Edge and in Firefox, then tick off SPEC §8's acceptance checklist. The build was checked in an embedded Chromium only. These haven't been tried yet:
   - the File System Access save and open dialogs, save-in-place after a reload, and real SVG and plan files on disk
   - Firefox's upload and download fallback
   - touch: dragging, pinch-to-zoom and tap-and-hold
   - phone widths (read-only)
   - copying to the clipboard (the embedded browser refused it; the manual-copy fallback worked)
2. **Answer the open questions** at the end of [decisions.md](decisions.md#still-open).

Later ideas (not v1) are listed at the end of [decisions.md](decisions.md#later-not-v1).

## Build and run

You need the .NET 10 SDK.

```
dotnet run --project Motif.Sketch.App      # http://localhost:5173
dotnet test                                 # the specs: the core through the User port, with fakes
dotnet publish Motif.Sketch.App -c Release  # static files in bin/Release/net10.0/publish/wwwroot
```

Pushing to `main` runs `.github/workflows/pages.yml`: it runs the specs, publishes, sets `<base href>` for the `/Motif.Sketch/` sub-path, adds `.nojekyll`, and deploys to GitHub Pages.

The layout is the one Motif Sketch exports for itself (the golden file `Checks/Motif.Sketch.Specs/Golden/Motif.Sketch.plan.md`):

| Folder | What's there |
|---|---|
| `Motif.Sketch.App/` | The Web App: the Blazor UI and the composition root |
| `Motif.Sketch/Motif.Sketch.Domain/` | The project and its rules, including how the sketch follows the context diagram |
| `Motif.Sketch/Motif.Sketch.UseCases/` | The ports, `MotifSketchApplication`, both layouts, the scenes and SVG writer, the plan, the file format, snapshot links and the example |
| `Adapters/` | One project per driven port, each with its small JS module |
| `Checks/` | `Motif.Sketch.Fakes` and `Motif.Sketch.Specs` |

## Files

| File | What it is |
|---|---|
| [SPEC.md](SPEC.md) | The requirements: architecture, file format, behaviour, quality bar, acceptance criteria |
| [docs/manual-checks.md](docs/manual-checks.md) | The browser checks to run before a release |
| [decisions.md](decisions.md) | Every design decision so far and why, plus ideas for later |
| [Motif Sketch - Context Diagram.contextsketch.json](<Motif Sketch - Context Diagram.contextsketch.json>) | Motif Sketch's context diagram (open it in Context Sketch) |
| [Motif Sketch.hexasketch.json](<Motif Sketch.hexasketch.json>) | Motif Sketch's ports-and-adapters sketch (open it in HexaSketch) |

The sketch is drawn with HexaSketch, which can't yet express two of Motif Sketch's ideas:
- **Merged sources:** the Viewer shows as "Not placed" even though the Web App serves it.
- **The Checks kind:** Specs shows as a Fake.

The adapter descriptions say so.

## License

MIT, see [LICENSE](LICENSE).
