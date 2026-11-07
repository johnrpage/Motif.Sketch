using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Motif.Sketch.Adapters;

namespace Motif.Sketch.App;

/// <summary>The Web App: the app bar, the three layouts, keys, and every project-wide command.</summary>
public partial class Shell
{
    private const string WelcomeDismissed = "motif-welcome-dismissed";

    [Inject] private BrowserFilesProjects Files { get; set; } = default!;

    private DotNetObjectReference<Shell>? self;
    private bool started;
    private string? menu;
    private bool helpOpen;
    private bool renamingTitle;
    private string theme = "system";
    private bool phone, narrow;
    private string layout = "context";
    private double split = 50;
    private int fitKey;
    private bool showWelcome;
    private string active = "context";
    private ExportCheck? exportCheck;
    private bool selectSystem;
    private ContextPane? contextPane;
    private SketchPane? sketchPane;
    private ElementReference work, divider;
    private long? resizing;
    private string? manualLink;

    private bool ReadOnlyUi => App.IsReadOnly || phone;
    private bool Editable => !ReadOnlyUi;

    private string EffectiveLayout => narrow && layout == "split" ? (active == "sketch" ? "sketch" : "context") : layout;

    private string ModeClass => (ReadOnlyUi ? "mode-viewer" : "mode-edit") + (narrow ? " narrow" : "") + (phone ? " phone" : "");

    private string PageTitle =>
        (App.HasUnsavedChanges && App.Mode == AppMode.Architect ? "• " : "") + App.Title + " · Motif Sketch";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Browser.Load();
            self = DotNetObjectReference.Create(this);
            theme = Browser.Theme();
            var viewport = Browser.WatchViewport(self);
            (phone, narrow) = (viewport[0], viewport[1]);
            Browser.ListenForKeys(self);
            await Files.Detect();
            Ui.Changed += Refresh;
            App.Changed += OnAppChanged;

            var result = await App.Start(Browser.Fragment());
            started = true;
            if (result.Problem is { } problem) Ui.Problem("That link can't be opened", [problem, "Here is your own work instead."]);
            if (result.StartedAs == StartedAs.Fresh)
            {
                showWelcome = Browser.Preference(WelcomeDismissed) != "1";
                selectSystem = !phone;
            }
            if (result.StartedAs == StartedAs.OtherTab)
                Ui.Say("Motif Sketch is open in another tab", "This tab is read-only. Use this tab instead to edit here.");
            layout = App.HasSketch && !narrow ? "split" : "context";
            fitKey++;
            StateHasChanged();
            return;
        }

        if (selectSystem && contextPane is not null)
        {
            selectSystem = false;
            contextPane.Select("element", App.Context.Elements.Single(e => e.Type == ElementType.System).Id);
            contextPane.Rename();
        }
        if (renamingTitle && focusTitle)
        {
            focusTitle = false;
            Browser.Focus("#title-edit");
        }
    }

    private bool focusTitle;

    private void OnAppChanged()
    {
        exportCheck = null;
        Browser.SetUnsaved(App.HasUnsavedChanges && App.Mode == AppMode.Architect);
        Refresh();
    }

    private void Refresh() => InvokeAsync(StateHasChanged);

    [JSInvokable]
    public void OnViewport(bool isPhone, bool isNarrow)
    {
        (phone, narrow) = (isPhone, isNarrow);
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnKey(string key)
    {
        if (!started) return;
        switch (key)
        {
            case "Escape":
                if (Ui.Pending is not null) Ui.Answer(false);
                else if (menu is not null || helpOpen) { menu = null; helpOpen = false; }
                else { contextPane?.Clear(); sketchPane?.Clear(); }
                break;
            case "Save" when Editable: await Save(); break;
            case "SaveAs" when Editable: await SaveAs(); break;
            case "Undo" when Editable: App.Undo(); break;
            case "Redo" when Editable: App.Redo(); break;
            case "Delete" when Editable:
                if (active == "sketch" && sketchPane is not null) await sketchPane.DeleteSelection();
                else contextPane?.DeleteSelection();
                break;
            case "Rename" when Editable:
                if (active == "sketch") sketchPane?.Rename(); else contextPane?.Rename();
                break;
            case "Layout1": SetLayout("context"); break;
            case "Layout2": SetLayout("split"); break;
            case "Layout3": SetLayout("sketch"); break;
        }
        StateHasChanged();
    }

    // ---- Layout --------------------------------------------------------------------------------

    private void SetLayout(string value)
    {
        if (value == "split" && narrow) value = active == "sketch" ? "sketch" : "context";
        if (value != "split") active = value;
        if (layout == value) return;
        layout = value;
        fitKey++;
    }

    private void StartResize(PointerEventArgs e)
    {
        resizing = e.PointerId;
        Browser.Capture(divider, e.PointerId);
    }

    private void Resize(PointerEventArgs e)
    {
        if (resizing != e.PointerId) return;
        var r = Browser.Rect(work);
        if (r[2] <= 0) return;
        split = Math.Clamp((e.ClientX - r[0]) / r[2] * 100, 20, 80);
    }

    private void EndResize(PointerEventArgs e)
    {
        if (resizing != e.PointerId) return;
        resizing = null;
        fitKey++;
    }

    // ---- Menus -------------------------------------------------------------------------------

    private void Toggle(string name)
    {
        menu = menu == name ? null : name;
        helpOpen = false;
    }

    private void OpenExportMenu()
    {
        exportCheck = App.CheckExport();
        Toggle("export");
    }

    private void CloseMenus() => menu = null;

    private void SetTheme(string value)
    {
        theme = value;
        Browser.SetTheme(value);
        menu = null;
    }

    private void StartTitleRename()
    {
        renamingTitle = true;
        focusTitle = true;
    }

    private void RenameTitle(ChangeEventArgs e)
    {
        renamingTitle = false;
        var title = e.Value?.ToString() ?? "";
        if (title.Trim() != App.Title) Ui.Run(() => App.RenameProject(title));
    }

    private void DismissWelcome()
    {
        showWelcome = false;
        Browser.SetPreference(WelcomeDismissed, "1");
    }

    private void ElementAdded() => showWelcome = false;

    private void SketchStarted()
    {
        Ui.Say("Sketch started", "Drag the items under Not placed onto ports, adapters or the hexagon.");
        SetLayout(narrow ? "sketch" : "split");
        fitKey++;
    }

    // ---- Project commands ----------------------------------------------------------------------

    private async Task<bool> MayDiscard(string doing)
    {
        menu = null;
        if (!App.HasUnsavedChanges) return true;
        return await Ui.Confirm("Discard unsaved changes?",
            $"{doing} replaces \"{App.Title}\", which has changes that aren't saved to a file.", "Discard changes", "Keep working");
    }

    private async Task New()
    {
        if (!await MayDiscard("A new project")) return;
        await App.NewProject();
        layout = "context";
        fitKey++;
        selectSystem = true;
    }

    private async Task Open()
    {
        if (!await MayDiscard("Opening a file")) return;
        var outcome = await App.Open();
        if (outcome.Cancelled) return;
        if (!outcome.Succeeded) { Ui.Problem("That file can't be opened", outcome.Problems); return; }
        showWelcome = false;
        layout = App.HasSketch && !narrow ? "split" : "context";
        fitKey++;
    }

    private async Task OpenExample()
    {
        if (!await MayDiscard("The example")) return;
        await App.OpenExample();
        showWelcome = false;
        layout = narrow ? "context" : "split";
        fitKey++;
        Ui.Say("This is Motif Sketch's own design", "It's ordinary unsaved work: change it, or save it as a file of your own.");
    }

    private async Task Save()
    {
        menu = null;
        var outcome = await App.Save();
        Ui.Report(outcome, App.CanSaveInPlace ? $"Saved to {App.FileName}" : $"Downloaded {App.FileName}", "The project couldn't be saved");
    }

    private async Task SaveAs()
    {
        menu = null;
        var outcome = await App.SaveAs();
        Ui.Report(outcome, App.CanSaveInPlace ? $"Saved to {App.FileName}" : $"Downloaded {App.FileName}", "The project couldn't be saved");
    }

    private async Task MakeCopy()
    {
        if (!await Ui.Confirm("Make this your own work?",
                "The copy replaces the work autosaved in this browser. Save that to a file first if you want to keep it.",
                "Make a copy")) return;
        await App.MakeCopyToEdit();
        Browser.ClearFragment();
        Ui.Say("This is now your own work", "It autosaves in this browser. Save it to a file to keep it.");
    }

    private async Task UseThisTab()
    {
        await App.UseThisTab();
        fitKey++;
        Ui.Say("Editing in this tab", "The other tab is read-only now.");
    }

    private async Task CopyLink()
    {
        var outcome = await App.CopyLink(Browser.PageUrl());
        if (!outcome.Succeeded) manualLink = outcome.Link;
        else if (outcome.IsLong)
            Ui.Warn("Link copied, but it's long", [$"It's {outcome.Link!.Length:N0} characters. Most chat and email tools manage, but some may cut it."]);
        else Ui.Say("Read-only link copied", "It's a snapshot: later edits need a new link.");
    }

    private async Task ExportContext()
    {
        menu = null;
        Ui.Report(await App.ExportContextDiagram(), "Context diagram exported", "The context diagram couldn't be exported");
    }

    private async Task ExportSketch()
    {
        menu = null;
        Ui.Report(await App.ExportSketch(), "Ports and adapters exported", "The sketch couldn't be exported");
    }

    private async Task ExportPlan()
    {
        menu = null;
        Ui.Report(await App.ExportPlan(), "Solution plan exported", "The plan can't be exported yet");
    }

    public void Dispose()
    {
        Ui.Changed -= Refresh;
        App.Changed -= OnAppChanged;
        self?.Dispose();
    }
}
