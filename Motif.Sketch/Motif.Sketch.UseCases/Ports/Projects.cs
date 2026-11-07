namespace Motif.Sketch;

/// <summary>Driven port: opens and saves project files.</summary>
public interface Projects
{
    /// <summary>True when Save can write back to the same file (File System Access); otherwise Save downloads a copy.</summary>
    bool CanSaveInPlace { get; }

    /// <summary>Asks the user for a project file. Null if they cancelled.</summary>
    Task<OpenedFile?> Open();

    /// <summary>
    /// Writes a project. With a handle, it writes back to that file (asking the browser for permission
    /// again if needed); without one, it asks where to save, or downloads. Null if the user cancelled.
    /// </summary>
    Task<SavedFile?> Save(string json, string suggestedName, string? handle);
}
