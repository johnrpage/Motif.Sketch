namespace Motif.Sketch.Fakes;

/// <summary>In-memory project files. Behaves like save-in-place browsers, or like download-only ones.</summary>
public sealed class FakeProjects : Projects
{
    private int handles;

    public bool CanSaveInPlace { get; set; } = true;

    /// <summary>What the next Open returns; null means the user cancels.</summary>
    public OpenedFile? NextOpen { get; set; }

    /// <summary>The name the user picks in the next save dialog; null means they cancel.</summary>
    public string? NextSaveAsName { get; set; } = "Picked.motifsketch.json";

    public bool Fail { get; set; }

    /// <summary>Every write: the text, where it went, and whether it was written in place or downloaded.</summary>
    public List<(string Json, string Name, string? Handle, bool Downloaded)> Writes { get; } = [];

    public Task<OpenedFile?> Open() => Fail ? throw new IOException("The disk is unwell.") : Task.FromResult(NextOpen);

    public Task<SavedFile?> Save(string json, string suggestedName, string? handle)
    {
        if (Fail) throw new IOException("The disk is full.");
        if (!CanSaveInPlace)
        {
            Writes.Add((json, suggestedName, null, true));
            return Task.FromResult<SavedFile?>(new SavedFile(suggestedName, null));
        }
        if (handle is not null)
        {
            var name = Writes.LastOrDefault(w => w.Handle == handle).Name ?? suggestedName;
            Writes.Add((json, name, handle, false));
            return Task.FromResult<SavedFile?>(new SavedFile(name, handle));
        }
        if (NextSaveAsName is not { } picked) return Task.FromResult<SavedFile?>(null);
        var newHandle = $"handle-{++handles}";
        Writes.Add((json, picked, newHandle, false));
        return Task.FromResult<SavedFile?>(new SavedFile(picked, newHandle));
    }

    /// <summary>A file the fake can hand to Open, with a handle as a save-in-place browser would give.</summary>
    public OpenedFile FileOf(string name, string text) => new(name, text, CanSaveInPlace ? $"handle-{++handles}" : null);
}
