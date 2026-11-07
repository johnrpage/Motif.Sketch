using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Motif.Sketch;
using Motif.Sketch.Adapters;
using Motif.Sketch.App;

// The composition root: the only place that knows which adapters are used.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Motif.Sketch.App.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<BrowserFilesProjects>();
builder.Services.AddSingleton<IndexedDBAutosave>();
builder.Services.AddSingleton<SvgFilesDiagramExports>();
builder.Services.AddSingleton<MarkdownPlanSolutionStubs>();
builder.Services.AddSingleton<BrowserClipboard>();
builder.Services.AddSingleton(sp => new MotifSketchApplication(
    sp.GetRequiredService<BrowserFilesProjects>(),
    sp.GetRequiredService<IndexedDBAutosave>(),
    sp.GetRequiredService<SvgFilesDiagramExports>(),
    sp.GetRequiredService<MarkdownPlanSolutionStubs>(),
    sp.GetRequiredService<BrowserClipboard>()));
builder.Services.AddSingleton<Browser>();
builder.Services.AddSingleton<Ui>();

await builder.Build().RunAsync();
