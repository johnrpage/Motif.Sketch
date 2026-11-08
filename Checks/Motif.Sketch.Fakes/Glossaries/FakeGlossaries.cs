namespace Motif.Sketch.Fakes;

/// <summary>In-memory glossary files. Behaves like save-in-place browsers, or like download-only ones.</summary>
public sealed class FakeGlossaries : Glossaries
{
    private int handles;

    public bool CanSaveInPlace { get; set; } = true;

    /// <summary>What the next Open returns; null means the user cancels.</summary>
    public OpenedFile? NextOpen { get; set; }

    /// <summary>The name the user picks in the next save dialog; null means they cancel.</summary>
    public string? NextSaveAsName { get; set; } = "glossary.md";

    public bool Fail { get; set; }

    /// <summary>Every write: the text, where it went, and whether it was written in place or downloaded.</summary>
    public List<(string Markdown, string Name, string? Handle, bool Downloaded)> Writes { get; } = [];

    public Task<OpenedFile?> Open() => Fail ? throw new IOException("The disk is unwell.") : Task.FromResult(NextOpen);

    public Task<SavedFile?> Save(string markdown, string suggestedName, string? handle)
    {
        if (Fail) throw new IOException("The disk is full.");
        if (!CanSaveInPlace)
        {
            Writes.Add((markdown, suggestedName, null, true));
            return Task.FromResult<SavedFile?>(new SavedFile(suggestedName, null));
        }
        if (handle is not null)
        {
            Writes.Add((markdown, suggestedName, handle, false));
            return Task.FromResult<SavedFile?>(new SavedFile(suggestedName, handle));
        }
        if (NextSaveAsName is not { } picked) return Task.FromResult<SavedFile?>(null);
        var newHandle = $"glossary-{++handles}";
        Writes.Add((markdown, picked, newHandle, false));
        return Task.FromResult<SavedFile?>(new SavedFile(picked, newHandle));
    }

    /// <summary>A glossary the fake can hand to Open, with a handle as a save-in-place browser would give.</summary>
    public OpenedFile FileOf(string text, string name = "glossary.md") =>
        new(name, text, CanSaveInPlace ? $"glossary-{++handles}" : null);
}
