namespace Motif.Sketch;

/// <summary>
/// Driven port: keeps the work in progress and its file handle between visits. Only one tab owns
/// autosave at a time; the others may read it but never write it.
/// </summary>
public interface Autosave
{
    /// <summary>Tries to make this tab the owner. False if another tab already owns autosave.</summary>
    Task<bool> Claim();

    /// <summary>Asks the owning tab to hand over, and waits until this tab owns autosave.</summary>
    Task TakeOver();

    /// <summary>Raised when another tab has taken over; this tab's last save has been written by then.</summary>
    event Action? Released;

    /// <summary>The autosaved work, or null if there is none.</summary>
    Task<AutosavedWork?> Restore();

    /// <summary>Replaces the autosaved work. Writes may be coalesced; the last one always lands.</summary>
    Task Store(AutosavedWork work);
}
