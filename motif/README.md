# motif/

This repo is designed with the [Motif.NET](https://github.com/motif-dotnet) tools. This folder holds their files: the design, the plan built from it, and the project's words. They're for people and coding agents alike.

| File | What it is | Who changes it |
|---|---|---|
| [glossary.md](glossary.md) | The project's terms: what each word means, and which words to avoid | People and agents, by hand |
| [sketch.motifsketch.json](sketch.motifsketch.json) | The design: the context diagram and the ports-and-adapters sketch | [Motif Sketch](https://motif-dotnet.github.io/Motif.Sketch/) only |
| [plan.md](plan.md) | The solution plan exported from the sketch: the projects, ports and adapters to build | Motif Sketch only (Export plan) |
| [diagrams/](diagrams/) | The two diagrams as SVG pictures, exported from the sketch | Motif Sketch only (Export) |

## For coding agents

1. **Read [glossary.md](glossary.md) first.** Use its terms in code, comments, docs and messages, and don't use the words it lists under **Avoid**. When you introduce a new concept, add an entry for it marked *proposed* in the same change. When something is renamed, rename its entry and move the old name to **Avoid**.
2. **Read [plan.md](plan.md)** for the shape of the solution. The code follows it.
3. **Don't edit the sketch, the plan or the diagrams by hand.** They're exported by Motif Sketch. If the design needs to change, say so, and a person changes the sketch.
4. **When the plan changes**, the diff of `plan.md` shows what to add, rename or remove in the code.

## In this repo

The sketch is also the app's bundled example, and the specs check that `plan.md` and the diagrams match what the sketch exports. After changing the sketch or an exporter, refresh them with:

```bash
UPDATE_GOLDEN=1 dotnet test
```
