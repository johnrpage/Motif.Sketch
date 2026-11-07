using Microsoft.JSInterop;

namespace Motif.Sketch.Adapters;

/// <summary>Autosave in IndexedDB; Web Locks and a BroadcastChannel decide which tab owns it.</summary>
public sealed class IndexedDBAutosave(IJSRuntime js) : Autosave, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> module = new(async () =>
    {
        var m = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Motif.Sketch.Autosave/autosave.js");
        return m;
    });
    private DotNetObjectReference<IndexedDBAutosave>? self;

    public event Action? Released;

    public async Task<bool> Claim() => await (await Module()).InvokeAsync<bool>("claim");

    public async Task TakeOver() => await (await Module()).InvokeVoidAsync("takeOver");

    public async Task<AutosavedWork?> Restore()
    {
        var work = await (await Module()).InvokeAsync<string?[]?>("restore");
        return work is null ? null : new AutosavedWork(work[0]!, work[1], work[2], !string.IsNullOrEmpty(work[3]));
    }

    public async Task Store(AutosavedWork work) =>
        await (await Module()).InvokeVoidAsync("store", work.Json, work.FileName, work.FileHandle, work.Unsaved);

    [JSInvokable]
    public void OnReleased() => Released?.Invoke();

    private async Task<IJSObjectReference> Module()
    {
        var m = await module.Value;
        if (self is null)
        {
            self = DotNetObjectReference.Create(this);
            await m.InvokeVoidAsync("init", self);
        }
        return m;
    }

    public async ValueTask DisposeAsync()
    {
        if (module.IsValueCreated) await (await module.Value).DisposeAsync();
        self?.Dispose();
    }
}
