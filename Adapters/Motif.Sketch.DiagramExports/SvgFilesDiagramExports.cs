using Microsoft.JSInterop;

namespace Motif.Sketch.Adapters;

/// <summary>Writes SVG pictures through the File System Access API where there is one, else downloads them.</summary>
public sealed class SvgFilesDiagramExports(IJSRuntime js) : DiagramExports, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> module =
        new(() => js.InvokeAsync<IJSObjectReference>("import", "./_content/Motif.Sketch.DiagramExports/diagramexports.js").AsTask());

    public async Task<bool> Write(string suggestedName, string svg) =>
        await (await module.Value).InvokeAsync<bool>("save", svg, suggestedName);

    public async ValueTask DisposeAsync()
    {
        if (module.IsValueCreated) await (await module.Value).DisposeAsync();
    }
}
