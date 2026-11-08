namespace Motif.Sketch;

public enum AppMode
{
    /// <summary>Full editing.</summary>
    Architect,
    /// <summary>A shared snapshot, read-only. Never touches autosave.</summary>
    Viewer,
    /// <summary>Another tab owns autosave, so this one is read-only.</summary>
    OtherTab,
}

/// <summary>What an export would warn about (it still goes ahead) or be blocked by (plan problems).</summary>
public sealed record ExportCheck(IReadOnlyList<string> Unplaced, IReadOnlyList<string> Warnings, IReadOnlyList<string> PlanProblems);

/// <summary>Driving port, query half: everything the user can see.</summary>
public interface UserQueries
{
    /// <summary>Raised after anything a query would answer differently.</summary>
    event Action? Changed;

    AppMode Mode { get; }
    bool IsReadOnly { get; }

    string Title { get; }
    /// <summary>Nothing but the system: no other elements and no sketch.</summary>
    bool IsEmpty { get; }
    /// <summary>The file the project was last opened from or saved to; null if never saved.</summary>
    string? FileName { get; }
    bool HasUnsavedChanges { get; }
    bool CanSaveInPlace { get; }
    bool CanUndo { get; }
    bool CanRedo { get; }

    ContextView Context { get; }
    Scene ContextScene { get; }

    bool HasSketch { get; }
    SketchView? Sketch { get; }
    Scene? SketchScene { get; }
    IReadOnlyList<UnplacedItem> Unplaced { get; }

    /// <summary>The context diagram as it would look with one element moved, for live dragging.</summary>
    Scene PreviewMove(string elementId, double x, double y);

    ExportCheck CheckExport();

    /// <summary>The project as it would be saved.</summary>
    string ProjectJsonText { get; }
}
