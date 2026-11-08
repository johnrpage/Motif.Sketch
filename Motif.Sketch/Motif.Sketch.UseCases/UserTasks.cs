namespace Motif.Sketch;

/// <summary>What happened when a task touched the outside world.</summary>
public sealed record Outcome(bool Succeeded, bool Cancelled, IReadOnlyList<string> Problems, IReadOnlyList<string> Warnings)
{
    public static Outcome Ok(IReadOnlyList<string>? warnings = null) => new(true, false, [], warnings ?? []);
    public static Outcome WasCancelled { get; } = new(false, true, [], []);
    public static Outcome Failed(IReadOnlyList<string> problems) => new(false, false, problems, []);
    public static Outcome Failed(string problem) => new(false, false, [problem], []);
}

/// <summary>How the app came up.</summary>
public enum StartedAs
{
    /// <summary>A blank project: no autosave and no link.</summary>
    Fresh,
    /// <summary>Work restored from autosave.</summary>
    Restored,
    /// <summary>A shared snapshot, read-only.</summary>
    Snapshot,
    /// <summary>The app is open in another tab, which owns autosave; this tab is read-only.</summary>
    OtherTab,
    /// <summary>The bundled example, read-only like a snapshot: from an <c>#example</c> link, or instead of empty work.</summary>
    Example,
}

/// <param name="Problem">Why a link couldn't be opened, if one was given and was bad.</param>
public sealed record StartResult(StartedAs StartedAs, string? Problem = null);

public sealed record LinkOutcome(bool Succeeded, string? Link, bool IsLong, string? Problem);

/// <summary>Driving port, command half: everything the user can do.</summary>
/// <remarks>
/// Every editing task is one undo step and throws <see cref="ProjectRuleException"/> (leaving the
/// project untouched) if it would break a rule. Ids are those shown by <see cref="UserQueries"/>.
/// </remarks>
public interface UserTasks
{
    /// <summary>
    /// Starts the app: the example for an <c>#example</c> link, a snapshot link if the fragment holds one,
    /// else the autosaved work, else a blank project. With <paramref name="exampleIfEmpty"/> (a phone, which
    /// can't edit), the example is shown read-only instead of a blank or empty project.
    /// </summary>
    Task<StartResult> Start(string? fragment, bool exampleIfEmpty = false);

    Task NewProject();
    Task<Outcome> Open();
    Task<Outcome> Save();
    Task<Outcome> SaveAs();
    Task OpenExample();

    /// <summary>Turns a shared snapshot into the visitor's own work, replacing their autosave.</summary>
    Task MakeCopyToEdit();

    /// <summary>Takes autosave over from the tab that owns it, and carries on with the latest work here.</summary>
    Task UseThisTab();

    Task<Outcome> ExportContextDiagram();
    Task<Outcome> ExportSketch();
    Task<Outcome> ExportPlan();
    Task<LinkOutcome> CopyLink(string pageUrl);

    void RenameProject(string title);

    // Context diagram
    string AddElement(ElementType type);
    void RenameElement(string elementId, string name);
    void DescribeElement(string elementId, string? description);
    void SetElementTechnology(string elementId, string? technology);
    void SetElementType(string elementId, ElementType type);
    void DeleteElement(string elementId);
    void MoveElement(string elementId, double x, double y);
    void SetRelationshipLabel(string relationshipId, string? label);
    void SetRelationshipDirection(string relationshipId, RelationshipDirection direction);
    void AutoLayout();
    void AutoLayoutAll();

    // Sketch
    void StartSketch();
    string AddPort(Side side);
    void RenamePort(string portId, string name);
    void DescribePort(string portId, string? description);
    void DeletePort(string portId);
    void MovePort(string portId, int indexOnSide);
    string Place(string elementId, string portId, int? index = null);
    string PlaceOnNewPort(string elementId, Side side);
    void MergeInto(string elementId, string adapterId);
    void RemoveSource(string adapterId, string elementId);
    string AddAdapter(string portId, AdapterKind kind = AdapterKind.Real);
    void MoveAdapter(string adapterId, string portId, int? index = null);
    void RemoveAdapter(string adapterId);
    void RenameAdapter(string adapterId, string name);
    void SetAdapterTechnology(string adapterId, string? technology);
    void DescribeAdapter(string adapterId, string? description);
    void SetAdapterKind(string adapterId, AdapterKind kind);

    void Undo();
    void Redo();
}
