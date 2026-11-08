# Glossary

The words this project uses, and what they mean. People and agents use these terms in code, docs, issues and discussion, and avoid the words listed under **Avoid**. A new concept gets an entry, marked *proposed*, in the same change that introduces it. An entry with no status is agreed.

The format is described in the [Motif Glossary design note](https://github.com/motif-dotnet/.github/blob/main/design/glossary.md). **Sketch** gives the id of the element or port in Motif Sketch's own design ([sketch.motifsketch.json](sketch.motifsketch.json)) that the term comes from.

## Adapter

One concrete way of plugging something into a port. Each adapter has a kind: real, fake, simulated or checks.

- **Kind:** domain concept
- **In code:** `Adapter`, `AdapterKind`
- **See also:** [Port](#port), [Source](#source)

## Architect

The person who sketches the context diagram and then the ports and adapters, exports the diagrams and the plan, and shares snapshot links. Also the name of the app's full editing mode.

- **Kind:** actor
- **Avoid:** user (that's the port), editor
- **In code:** `AppMode.Architect`
- **Sketch:** `c5a10a02`

## Autosave

Keeps the work in progress, and the handle of the file it came from, in the browser between visits, so a refresh loses nothing. Only one tab owns autosave at a time. A safety net, not saving: work is still unsaved until it's written to a file.

- **Kind:** port
- **In code:** `Autosave`
- **Sketch:** `d6b20c03`
- **See also:** [Browser Storage](#browser-storage)

## Browser Storage

Where autosaved work is kept: the browser's IndexedDB.

- **Kind:** storage
- **Sketch:** `c5a10a06`
- **See also:** [Autosave](#autosave)

## Checks adapter

An adapter that drives the application through a driving port and checks what it does. Allowed on driving ports only. Every driving port starts with one, named Specs.

- **Kind:** domain concept
- **Avoid:** fake (a fake stands in for something the application calls; a checks adapter calls the application)
- **In code:** `AdapterKind.Checks`; the `Checks/` folder
- **See also:** [Specs](#specs)

## Clipboard

Where Copy link puts a snapshot link, ready to paste. The same name is used for the external system and for the port the app reaches it through.

- **Kind:** external system, port
- **In code:** `Clipboard`
- **Sketch:** `c5a10a0a` (element), `d6b20c06` (port)

## Context diagram

The first view: the system in the middle, with the actors, external systems and storage around it, each joined to the system by one relationship. Layout is automatic.

- **Kind:** domain concept
- **Avoid:** context map (a different thing in domain-driven design)
- **In code:** `ContextDiagram`
- **See also:** [Element](#element), [Relationship](#relationship)

## Diagram Exports

Writes the context diagram and the sketch as SVG pictures, cropped to the content and always in the light palette. The same name is used for the storage they're written to and for the port.

- **Kind:** storage, port
- **In code:** `DiagramExports`
- **Sketch:** `c5a10a07` (element), `d6b20c04` (port)

## Driving and driven

The two sides of the hexagon. A driving port is for things that use the application, and sits on the left. A driven port is for things the application uses, and sits on the right.

- **Kind:** domain concept
- **Avoid:** primary and secondary (those are actor types), inbound and outbound
- **In code:** `Side.Driving`, `Side.Driven`

## Element

Anything on the context diagram: the system, a primary or secondary actor, an external system, or storage.

- **Kind:** domain concept
- **Avoid:** node, box
- **In code:** `Element`, `ElementType`
- **Status:** proposed

## Example

The bundled project: Motif Sketch's own design, with both views. Opened with File → Open example or an `#example` link.

- **Kind:** domain concept
- **Avoid:** sample, demo
- **In code:** `Example`, `StartedAs.Example`

## External system

Another system the system talks to, drawn on the right of the context diagram.

- **Kind:** domain concept
- **In code:** `ElementType.ExternalSystem`

## Fake

A simple in-process stand-in for a real adapter, for fast specs. Named "Fake ⟨Port⟩" by default.

- **Kind:** domain concept
- **Avoid:** mock, stub (stub means something else here: see [Solution Stubs](#solution-stubs))
- **In code:** `AdapterKind.Fake`; `Fake<Port>` classes in `Motif.Sketch.Fakes`

## Motif Sketch

This app: the first tool in the Motif.NET suite. Also the system on its own context diagram.

- **Kind:** system
- **Avoid:** Context Sketch, HexaSketch (the retired desktop tools it replaced)
- **In code:** the root namespace `Motif.Sketch`
- **Sketch:** `c5a10a01`

## Pinned

An element the user has dragged. Auto-layout leaves pinned elements where they are; Auto-layout all releases every pin first.

- **Kind:** domain concept
- **In code:** `Element.Pinned`

## Planned project

A .NET project in the solution plan, such as `Motif.Sketch.Domain`. Not the same thing as a project.

- **Kind:** domain concept
- **Avoid:** project, on its own
- **In code:** `PlannedProject`, `ProjectRole`
- **Status:** proposed
- **See also:** [Project](#project)

## Port

A named role the application plays or needs, on the driving or driven side. Adapters plug into it. In code, a port is an interface named plainly, with no `I` prefix.

- **Kind:** domain concept
- **Avoid:** interface (that's how a port appears in code, not what it is)
- **In code:** `Port`
- **See also:** [Adapter](#adapter), [Driving and driven](#driving-and-driven)

## Primary actor

A person or system that starts interactions with the system. Drawn on the left of the context diagram. Starting the sketch makes each primary actor the source of a real adapter on the User port.

- **Kind:** domain concept
- **In code:** `ElementType.PrimaryActor`

## Project

One system's context diagram and, once started, its sketch, saved together as one `.motifsketch.json` file.

- **Kind:** domain concept
- **Avoid:** document, workspace, diagram (a project holds two)
- **In code:** `Project`
- **See also:** [Projects](#projects), [Planned project](#planned-project)

## Project Files

Where projects are saved: local `.motifsketch.json` files, saved in place or downloaded.

- **Kind:** storage
- **Sketch:** `c5a10a05`
- **See also:** [Projects](#projects)

## Projects

The port that opens and saves project files.

- **Kind:** port
- **In code:** `Projects`
- **Sketch:** `d6b20c02`

## Real

The production adapter.

- **Kind:** domain concept
- **In code:** `AdapterKind.Real`

## Relationship

The line joining an element to the system. Every element except the system has exactly one. Its arrow points from the side that starts the interaction; two-way is for the rare case where both do.

- **Kind:** domain concept
- **Avoid:** link (that's a snapshot link), edge, connection
- **In code:** `Relationship`, `RelationshipDirection`

## Secondary actor

A person or system the system starts interactions with. Drawn on the right of the context diagram.

- **Kind:** domain concept
- **In code:** `ElementType.SecondaryActor`

## Simulated

A higher-fidelity stand-in for a real adapter, for people running the whole app locally.

- **Kind:** domain concept
- **In code:** `AdapterKind.Simulated`

## Sketch

The ports-and-adapters view: the hexagon, with ports around it and adapters on the ports. Absent from a project until the user starts it.

- **Kind:** domain concept
- **Avoid:** hexagon (that's the shape it's drawn with), and "sketch" on its own for the app (say Motif Sketch)
- **In code:** `Sketch`

## Snapshot link

A link that carries a whole project inside its URL, so it can be shared without a file or a server. It's a snapshot: later edits need a new link. It opens in Viewer mode.

- **Kind:** domain concept
- **Avoid:** share link, permalink
- **In code:** `SnapshotLink`
- **See also:** [Viewer](#viewer)

## Solution plan

The Markdown plan Motif Sketch exports: the .NET solution a coding agent stubs out from the sketch, with its projects, ports and adapters named.

- **Kind:** domain concept
- **Avoid:** spec, stub (the plan describes the stubs; it isn't one)
- **In code:** `SolutionPlan`, `SolutionPlanner`, `PlanMarkdown`
- **Sketch:** `c5a10a09` (the storage is named Solution Plans)
- **See also:** [Solution Stubs](#solution-stubs)

## Solution Stubs

The port the solution plan is handed to, to be built. Today it writes a Markdown file; a later version may add a GitHub adapter that builds the stubs in a repo.

- **Kind:** port
- **In code:** `SolutionStubs`
- **Sketch:** `d6b20c05`

## Source

A context diagram element that an adapter stands for. An adapter can have several sources or none, and an element can be the source of at most one adapter.

- **Kind:** domain concept
- **In code:** `Adapter.Sources`

## Specs

The checks adapter on a driving port, named Specs by default. Motif Sketch's own Specs are xUnit tests that drive the app through the User port with fakes.

- **Kind:** adapter
- **Avoid:** confusing it with `SPEC.md`, which is the app's specification
- **In code:** `Motif.Sketch.Specs`
- **See also:** [Checks adapter](#checks-adapter)

## Storage

Somewhere the system keeps data, drawn on the right of the context diagram, below the external systems.

- **Kind:** domain concept
- **In code:** `ElementType.Storage`

## System

The one element at the centre of the context diagram: the thing being designed. Created with the project, and can't be deleted.

- **Kind:** domain concept
- **In code:** `ElementType.System`

## Unplaced list

The elements not yet placed in the sketch. Dragging one onto a port, the hexagon or an adapter places it.

- **Kind:** domain concept
- **In code:** `UnplacedItem`

## User port

The driving port people use. Always present in a sketch, and can't be deleted. Its real adapter here is the Web App.

- **Kind:** port
- **Avoid:** user, for a person (say Architect or Viewer)
- **In code:** `UserTasks`, `UserQueries`
- **Sketch:** `d6b20c01`

## Viewer

Someone who opens a snapshot link to look at a shared project, read-only. Also the read-only mode the app opens in for them, which never touches autosave.

- **Kind:** actor
- **Avoid:** reader, guest
- **In code:** `AppMode.Viewer`
- **Sketch:** `c5a10a08`

## Web App

The real adapter on the User port: the Blazor WebAssembly UI.

- **Kind:** adapter
- **In code:** `Motif.Sketch.App`
- **See also:** [User port](#user-port)
