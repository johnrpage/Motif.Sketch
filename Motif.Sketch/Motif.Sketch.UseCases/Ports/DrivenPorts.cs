namespace Motif.Sketch;

/// <summary>A project file as read: its name, its text, and a handle to write back to it, if the browser gave one.</summary>
/// <remarks>
/// File handles are opaque strings. The adapter that gets a real handle from the browser keeps it on
/// its side, so another adapter (Autosave) can store and restore it without seeing it.
/// </remarks>
public sealed record OpenedFile(string Name, string Text, string? Handle);

/// <summary>Where a save went: the file's name, and its handle if it can be written again in place.</summary>
public sealed record SavedFile(string Name, string? Handle);

/// <summary>Work in progress kept between visits, with the file it belongs to and whether it was saved there.</summary>
public sealed record AutosavedWork(string Json, string? FileName, string? FileHandle, bool Unsaved);
