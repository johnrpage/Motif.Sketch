namespace Motif.Sketch.App;

public enum ToastKind { Info, Warning, Problem }

public sealed record Toast(int Id, ToastKind Kind, string Title, IReadOnlyList<string> Lines);

/// <param name="Danger">The confirming action loses something, so it's drawn in the accent colour.</param>
public sealed record Confirmation(string Title, string Message, string Confirm, string Cancel, bool Danger, TaskCompletionSource<bool> Answer);

/// <summary>Toasts and confirmations: the friendly messages around every command.</summary>
public sealed class Ui
{
    private int next;

    public event Action? Changed;

    public List<Toast> Toasts { get; } = [];
    public Confirmation? Pending { get; private set; }

    public void Say(string title, params string[] lines) => Show(ToastKind.Info, title, lines);
    public void Warn(string title, IEnumerable<string> lines) => Show(ToastKind.Warning, title, [.. lines]);
    public void Problem(string title, IEnumerable<string> lines) => Show(ToastKind.Problem, title, [.. lines]);

    public void Dismiss(int id)
    {
        Toasts.RemoveAll(t => t.Id == id);
        Changed?.Invoke();
    }

    public Task<bool> Confirm(string title, string message, string confirm, string cancel = "Cancel", bool danger = true)
    {
        Pending?.Answer.TrySetResult(false);
        Pending = new Confirmation(title, message, confirm, cancel, danger, new TaskCompletionSource<bool>());
        Changed?.Invoke();
        return Pending.Answer.Task;
    }

    public void Answer(bool yes)
    {
        var pending = Pending;
        Pending = null;
        pending?.Answer.TrySetResult(yes);
        Changed?.Invoke();
    }

    /// <summary>Runs an edit, turning a broken rule into a friendly message instead of a crash.</summary>
    public bool Run(Action edit)
    {
        try
        {
            edit();
            return true;
        }
        catch (ProjectRuleException e)
        {
            Problem("That can't be done", [e.Message]);
            return false;
        }
    }

    /// <summary>Reports an outcome: problems, warnings, or a short note that it worked.</summary>
    public void Report(Outcome outcome, string done, string failed)
    {
        if (outcome.Cancelled) return;
        if (!outcome.Succeeded) Problem(failed, outcome.Problems);
        else if (outcome.Warnings.Count > 0) Warn(done, outcome.Warnings);
        else Say(done);
    }

    private void Show(ToastKind kind, string title, string[] lines)
    {
        var toast = new Toast(++next, kind, title, lines);
        Toasts.Add(toast);
        if (Toasts.Count > 4) Toasts.RemoveAt(0);
        Changed?.Invoke();
        _ = Expire(toast);
    }

    private async Task Expire(Toast toast)
    {
        await Task.Delay(toast.Kind == ToastKind.Info ? 3500 : 9000);
        Dismiss(toast.Id);
    }
}
