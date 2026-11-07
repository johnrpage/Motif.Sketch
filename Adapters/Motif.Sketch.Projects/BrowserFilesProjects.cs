using Microsoft.JSInterop;

namespace Motif.Sketch.Adapters;

/// <summary>Project files through the File System Access API where the browser has it, else upload and download.</summary>
public sealed class BrowserFilesProjects(IJSRuntime js) : Projects, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> module =
        new(() => js.InvokeAsync<IJSObjectReference>("import", "./_content/Motif.Sketch.Projects/projects.js").AsTask());

    /// <summary>Set once at startup by feature detection (see <see cref="Detect"/>).</summary>
    public bool CanSaveInPlace { get; private set; }

    public async Task Detect() => CanSaveInPlace = await (await module.Value).InvokeAsync<bool>("canSaveInPlace");

    public async Task<OpenedFile?> Open()
    {
        var file = await (await module.Value).InvokeAsync<string?[]?>("open");
        return file is null ? null : new OpenedFile(file[0]!, file[1]!, file[2]);
    }

    public async Task<SavedFile?> Save(string json, string suggestedName, string? handle)
    {
        var saved = await (await module.Value).InvokeAsync<string?[]?>("save", json, suggestedName, handle);
        return saved is null ? null : new SavedFile(saved[0]!, saved[1]);
    }

    public async ValueTask DisposeAsync()
    {
        if (module.IsValueCreated) await (await module.Value).DisposeAsync();
    }
}
