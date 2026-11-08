namespace Motif.Sketch;

/// <summary>Driven port: opens a project's glossary (<c>motif/glossary.md</c>) and writes it back.</summary>
public interface Glossaries
{
    /// <summary>True when Save can write back to the file that was opened; otherwise it downloads a copy.</summary>
    bool CanSaveInPlace { get; }

    /// <summary>Asks the user for a glossary file. Null if they cancelled.</summary>
    Task<OpenedFile?> Open();

    /// <summary>
    /// Writes a glossary. With a handle, it writes back to that file; without one, it asks where to save,
    /// or downloads. Null if the user cancelled.
    /// </summary>
    Task<SavedFile?> Save(string markdown, string suggestedName, string? handle);
}
