namespace Motif.Sketch;

/// <summary>The application: implements the User port on top of the domain and the driven ports.</summary>
/// <remarks>
/// Undo works on whole-project snapshots: every edit clones the project, changes the clone and keeps
/// the old one, so a change and the sketch's follow-up to it (SPEC §5.3) are one step, in both views.
/// </remarks>
public sealed class MotifSketchApplication : UserTasks, UserQueries
{
    public const int HistoryLimit = 200;

    private readonly Projects projects;
    private readonly Autosave autosave;
    private readonly DiagramExports diagramExports;
    private readonly SolutionStubs solutionStubs;
    private readonly Clipboard clipboard;

    private readonly LinkedList<(Project Project, int Version)> undo = new();
    private readonly Stack<(Project Project, int Version)> redo = new();
    private Project project;
    private int version;
    private int savedVersion;
    private int lastVersion;
    private string? fileHandle;
    private bool ownsAutosave;
    private Views? views;

    public MotifSketchApplication(
        Projects projects, Autosave autosave, DiagramExports diagramExports, SolutionStubs solutionStubs, Clipboard clipboard)
    {
        this.projects = projects;
        this.autosave = autosave;
        this.diagramExports = diagramExports;
        this.solutionStubs = solutionStubs;
        this.clipboard = clipboard;
        project = Blank();
        autosave.Released += OnReleased;
    }

    public event Action? Changed;

    // ---- Queries ---------------------------------------------------------------------------

    public AppMode Mode { get; private set; } = AppMode.Architect;
    public bool IsReadOnly => Mode != AppMode.Architect;
    public string Title => project.Title;
    public bool IsEmpty => IsEmptyProject(project);
    public string? FileName { get; private set; }
    public bool HasUnsavedChanges => version != savedVersion;
    public bool CanSaveInPlace => projects.CanSaveInPlace;
    public bool CanUndo => !IsReadOnly && undo.Count > 0;
    public bool CanRedo => !IsReadOnly && redo.Count > 0;

    public ContextView Context => Current().Context;
    public Scene ContextScene => Current().ContextScene;
    public bool HasSketch => project.Sketch is not null;
    public SketchView? Sketch => Current().Sketch;
    public Scene? SketchScene => Current().SketchScene;
    public IReadOnlyList<UnplacedItem> Unplaced => Current().Unplaced;
    public string ProjectJsonText => ProjectJson.Save(project);

    public Scene PreviewMove(string elementId, double x, double y)
    {
        var copy = project.Clone();
        copy.MoveElement(elementId, x, y);
        return global::Motif.Sketch.ContextScene.Of(ContextView.Of(copy.ContextDiagram, copy.Title));
    }

    public ExportCheck CheckExport()
    {
        var unplaced = project.Unplaced.Select(e => e.Name).ToList();
        if (project.Sketch is null) return new ExportCheck(unplaced, [], []);
        var plan = SolutionPlanner.Plan(project);
        return new ExportCheck(unplaced, plan.Warnings, plan.Problems);
    }

    // ---- Starting, files and autosave --------------------------------------------------------

    public async Task<StartResult> Start(string? fragment, bool exampleIfEmpty = false)
    {
        // Like a snapshot, the example opened this way never touches autosave.
        if (Example.IsLink(fragment)) return StartExample();

        string? linkProblem = null;
        if (SnapshotLink.IsSnapshot(fragment))
        {
            var snapshot = SnapshotLink.Read(fragment!);
            if (snapshot.Project is { } shared)
            {
                // Viewer mode never touches autosave, so it can't overwrite the visitor's own work.
                Mode = AppMode.Viewer;
                Begin(shared, fileName: null, handle: null, saved: true);
                return new StartResult(StartedAs.Snapshot);
            }
            linkProblem = snapshot.Problem;
        }

        if (exampleIfEmpty && linkProblem is null && await AutosaveIsEmpty()) return StartExample();

        ownsAutosave = await autosave.Claim();
        var restored = await RestoreAutosave();
        Mode = ownsAutosave ? AppMode.Architect : AppMode.OtherTab;
        if (!restored) Begin(Blank(), null, null, saved: true);
        Raise();
        return new StartResult(
            !ownsAutosave ? StartedAs.OtherTab : restored ? StartedAs.Restored : StartedAs.Fresh,
            linkProblem);
    }

    private StartResult StartExample()
    {
        Mode = AppMode.Viewer;
        Begin(Example.Project(), fileName: null, handle: null, saved: true);
        return new StartResult(StartedAs.Example);
    }

    /// <summary>True if there's no autosaved work, or it's unreadable, or it's only the system.</summary>
    private async Task<bool> AutosaveIsEmpty()
    {
        try
        {
            var work = await autosave.Restore();
            return work is null || ProjectJson.Load(work.Json).Project is not { } restored || IsEmptyProject(restored);
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static bool IsEmptyProject(Project p) => p.ContextDiagram.Elements.Count == 1 && p.Sketch is null;

    public Task NewProject()
    {
        RequireEditable();
        Begin(Blank(), null, null, saved: true);
        return Persist();
    }

    public async Task OpenExample()
    {
        RequireEditable();
        Begin(Example.Project(), null, null, saved: false);
        await Persist();
    }

    public async Task<Outcome> Open()
    {
        RequireEditable();
        OpenedFile? file;
        try
        {
            file = await projects.Open();
        }
        catch (Exception e)
        {
            return Outcome.Failed($"The file couldn't be opened: {e.Message}");
        }
        if (file is null) return Outcome.WasCancelled;

        var loaded = ProjectJson.Load(file.Text);
        if (loaded.Project is not { } opened)
            return Outcome.Failed(loaded.Problems);
        if (loaded.NeedsLayout) ContextLayout.Apply(opened.ContextDiagram);
        Begin(opened, file.Name, file.Handle, saved: true);
        await Persist();
        return Outcome.Ok();
    }

    public Task<Outcome> Save() => SaveTo(fileHandle);

    public Task<Outcome> SaveAs() => SaveTo(null);

    private async Task<Outcome> SaveTo(string? handle)
    {
        RequireEditable();
        var savingVersion = version;
        SavedFile? saved;
        try
        {
            saved = await projects.Save(ProjectJson.Save(project), FileName ?? SuggestedName(ProjectJson.Extension), handle);
        }
        catch (Exception e)
        {
            return Outcome.Failed($"The project couldn't be saved: {e.Message}");
        }
        if (saved is null) return Outcome.WasCancelled;

        FileName = saved.Name;
        fileHandle = saved.Handle;
        savedVersion = savingVersion;
        Raise();
        await Persist();
        return Outcome.Ok();
    }

    public async Task MakeCopyToEdit()
    {
        if (Mode != AppMode.Viewer) return;
        ownsAutosave = await autosave.Claim();
        if (!ownsAutosave)
        {
            await autosave.TakeOver();
            ownsAutosave = true;
        }
        Mode = AppMode.Architect;
        FileName = null;
        fileHandle = null;
        savedVersion = -1;
        Raise();
        await Persist();
    }

    public async Task UseThisTab()
    {
        if (Mode != AppMode.OtherTab) return;
        await autosave.TakeOver();
        ownsAutosave = true;
        Mode = AppMode.Architect;
        if (!await RestoreAutosave()) Begin(project, FileName, fileHandle, saved: !HasUnsavedChanges);
        Raise();
    }

    private void OnReleased()
    {
        if (Mode != AppMode.Architect) return;
        ownsAutosave = false;
        Mode = AppMode.OtherTab;
        Raise();
    }

    private async Task<bool> RestoreAutosave()
    {
        AutosavedWork? work;
        try
        {
            work = await autosave.Restore();
        }
        catch (Exception)
        {
            return false;
        }
        if (work is null || ProjectJson.Load(work.Json).Project is not { } restored) return false;
        Begin(restored, work.FileName, work.FileHandle, saved: !work.Unsaved);
        return true;
    }

    /// <summary>Writes the current work to autosave, if this tab owns it. Viewer mode never does.</summary>
    private async Task Persist()
    {
        if (Mode != AppMode.Architect || !ownsAutosave) return;
        try
        {
            await autosave.Store(new AutosavedWork(ProjectJson.Save(project), FileName, fileHandle, HasUnsavedChanges));
        }
        catch (Exception)
        {
            // Autosave is a safety net; a failed write mustn't stop the edit.
        }
    }

    // ---- Exports and sharing -----------------------------------------------------------------

    public Task<Outcome> ExportContextDiagram() =>
        Export(() => diagramExports.Write(SuggestedName(" - Context Diagram.svg"), SvgWriter.Write(ContextScene)),
            UnplacedWarnings());

    public Task<Outcome> ExportSketch()
    {
        if (SketchScene is not { } scene) return Task.FromResult(Outcome.Failed("Start the sketch first."));
        return Export(() => diagramExports.Write(SuggestedName(" - Ports and Adapters.svg"), SvgWriter.Write(scene)),
            UnplacedWarnings());
    }

    public Task<Outcome> ExportPlan()
    {
        if (project.Sketch is null) return Task.FromResult(Outcome.Failed("Start the sketch first: the plan is made from it."));
        var plan = SolutionPlanner.Plan(project);
        if (plan.Problems.Count > 0) return Task.FromResult(Outcome.Failed(plan.Problems));
        var markdown = PlanMarkdown.Write(plan);
        return Export(() => solutionStubs.Write(SuggestedName(" - plan.md"), markdown), plan.Warnings);
    }

    public async Task<LinkOutcome> CopyLink(string pageUrl)
    {
        var link = SnapshotLink.Link(pageUrl, project);
        try
        {
            await clipboard.Copy(link);
        }
        catch (Exception e)
        {
            return new LinkOutcome(false, link, false, $"The link couldn't be copied: {e.Message}");
        }
        return new LinkOutcome(true, link, link.Length > SnapshotLink.LongLinkThreshold, null);
    }

    private static async Task<Outcome> Export(Func<Task<bool>> write, IReadOnlyList<string> warnings)
    {
        try
        {
            return await write() ? Outcome.Ok(warnings) : Outcome.WasCancelled;
        }
        catch (Exception e)
        {
            return Outcome.Failed($"The export couldn't be written: {e.Message}");
        }
    }

    private List<string> UnplacedWarnings()
    {
        var unplaced = project.Unplaced;
        return unplaced.Count switch
        {
            0 => [],
            1 => [$"'{unplaced[0].Name}' isn't placed in the sketch."],
            _ => [$"{unplaced.Count} items aren't placed in the sketch."],
        };
    }

    private string SuggestedName(string suffix)
    {
        var invalid = new HashSet<char>(Path.GetInvalidFileNameChars().Concat("<>:\"/\\|?*"));
        var name = new string(project.Title.Select(c => invalid.Contains(c) ? '-' : c).ToArray()).Trim();
        return (name.Length == 0 ? "Untitled" : name) + suffix;
    }

    // ---- Editing -----------------------------------------------------------------------------

    public void RenameProject(string title) => Edit(p => p.Rename(title));

    public string AddElement(ElementType type) => Edit(p =>
    {
        var id = p.AddElement(type).Id;
        ContextLayout.Apply(p.ContextDiagram);
        return id;
    });

    public void RenameElement(string elementId, string name) => Edit(p => p.RenameElement(elementId, name));
    public void DescribeElement(string elementId, string? description) => Edit(p => p.DescribeElement(elementId, description));
    public void SetElementTechnology(string elementId, string? technology) => Edit(p => p.SetElementTechnology(elementId, technology));
    public void SetCodeRoot(string? codeRoot) => Edit(p => p.SetCodeRoot(codeRoot));

    public void SetElementType(string elementId, ElementType type) => Edit(p =>
    {
        p.SetElementType(elementId, type);
        ContextLayout.Apply(p.ContextDiagram);
    });

    public void DeleteElement(string elementId) => Edit(p =>
    {
        p.DeleteElement(elementId);
        ContextLayout.Apply(p.ContextDiagram);
    });

    public void MoveElement(string elementId, double x, double y) => Edit(p => p.MoveElement(elementId, Math.Round(x), Math.Round(y)));

    /// <summary>A longer label can widen the gap to the system, so the layout reruns for unpinned elements.</summary>
    public void SetRelationshipLabel(string relationshipId, string? label) => Edit(p =>
    {
        p.SetRelationshipLabel(relationshipId, label);
        ContextLayout.Apply(p.ContextDiagram);
    });

    public void SetRelationshipDirection(string relationshipId, RelationshipDirection direction) =>
        Edit(p => p.SetRelationshipDirection(relationshipId, direction));

    public void AutoLayout()
    {
        RequireEditable();
        var probe = project.Clone();
        if (ContextLayout.Apply(probe.ContextDiagram)) Edit(p => ContextLayout.Apply(p.ContextDiagram));
    }

    public void AutoLayoutAll() => Edit(p =>
    {
        p.ReleasePins();
        ContextLayout.Apply(p.ContextDiagram);
    });

    public void StartSketch() => Edit(p => p.StartSketch());
    public string AddPort(Side side) => Edit(p => p.AddPort(side).Id);
    public void RenamePort(string portId, string name) => Edit(p => p.RenamePort(portId, name));
    public void DescribePort(string portId, string? description) => Edit(p => p.DescribePort(portId, description));
    public void DeletePort(string portId) => Edit(p => p.DeletePort(portId));
    public void MovePort(string portId, int indexOnSide) => Edit(p => p.MovePort(portId, indexOnSide));
    public string Place(string elementId, string portId, int? index = null) => Edit(p => p.Place(elementId, portId, index).Id);
    public string PlaceOnNewPort(string elementId, Side side) => Edit(p => p.PlaceOnNewPort(elementId, side).Id);
    public void MergeInto(string elementId, string adapterId) => Edit(p => p.MergeInto(elementId, adapterId));
    public void RemoveSource(string adapterId, string elementId) => Edit(p => p.RemoveSource(adapterId, elementId));
    public string AddAdapter(string portId, AdapterKind kind = AdapterKind.Real) => Edit(p => p.AddAdapter(portId, kind).Id);
    public void MoveAdapter(string adapterId, string portId, int? index = null) => Edit(p => p.MoveAdapter(adapterId, portId, index));
    public void RemoveAdapter(string adapterId) => Edit(p => p.RemoveAdapter(adapterId));
    public void RenameAdapter(string adapterId, string name) => Edit(p => p.RenameAdapter(adapterId, name));
    public void SetAdapterTechnology(string adapterId, string? technology) => Edit(p => p.SetAdapterTechnology(adapterId, technology));
    public void DescribeAdapter(string adapterId, string? description) => Edit(p => p.DescribeAdapter(adapterId, description));
    public void SetAdapterKind(string adapterId, AdapterKind kind) => Edit(p => p.SetAdapterKind(adapterId, kind));

    public void Undo()
    {
        if (!CanUndo) return;
        redo.Push((project, version));
        (project, version) = undo.Last!.Value;
        undo.RemoveLast();
        AfterChange();
    }

    public void Redo()
    {
        if (!CanRedo) return;
        undo.AddLast((project, version));
        (project, version) = redo.Pop();
        AfterChange();
    }

    private void Edit(Action<Project> change) => Edit(p => { change(p); return 0; });

    /// <summary>Applies a change to a copy, so a change that breaks a rule leaves the project untouched.</summary>
    private T Edit<T>(Func<Project, T> change)
    {
        RequireEditable();
        var next = project.Clone();
        var result = change(next);
        undo.AddLast((project, version));
        if (undo.Count > HistoryLimit) undo.RemoveFirst();
        redo.Clear();
        project = next;
        version = ++lastVersion;
        AfterChange();
        return result;
    }

    private void AfterChange()
    {
        Raise();
        _ = Persist();
    }

    private void RequireEditable()
    {
        if (IsReadOnly)
            throw new ProjectRuleException(Mode == AppMode.Viewer
                ? "This is a shared snapshot, so it's read-only. Make a copy to edit it."
                : "Motif Sketch is open in another tab, so this one is read-only.");
    }

    private void Begin(Project started, string? fileName, string? handle, bool saved)
    {
        project = started;
        FileName = fileName;
        fileHandle = handle;
        undo.Clear();
        redo.Clear();
        version = ++lastVersion;
        savedVersion = saved ? version : -1;
        Raise();
    }

    private static Project Blank()
    {
        var blank = Project.CreateNew();
        ContextLayout.Apply(blank.ContextDiagram);
        return blank;
    }

    private void Raise() => Changed?.Invoke();

    // ---- Cached views --------------------------------------------------------------------------

    private Views Current()
    {
        if (views is { } cached && cached.Version == version && ReferenceEquals(cached.Project, project)) return cached;
        views = new Views(project, version);
        return views;
    }

    private sealed class Views(Project project, int version)
    {
        public Project Project { get; } = project;
        public int Version { get; } = version;

        private ContextView? context;
        private Scene? contextScene;
        private SketchView? sketch;
        private Scene? sketchScene;
        private IReadOnlyList<UnplacedItem>? unplaced;

        public ContextView Context => context ??= ContextView.Of(Project.ContextDiagram, Project.Title);
        public Scene ContextScene => contextScene ??= global::Motif.Sketch.ContextScene.Of(Context);
        public SketchView? Sketch => Project.Sketch is null ? null : sketch ??= SketchLayout.Of(Project);
        public Scene? SketchScene => Sketch is null ? null : sketchScene ??= global::Motif.Sketch.SketchScene.Of(Sketch);

        public IReadOnlyList<UnplacedItem> Unplaced => unplaced ??=
            Project.Unplaced.Select(e => new UnplacedItem(e.Id, e.Name, e.Type, e.Technology)).ToList();
    }
}
