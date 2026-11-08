using Motif.Suite;

namespace Motif.Sketch.App;

/// <param name="Danger">The confirming action loses something, so it's drawn in the accent colour.</param>
public sealed record Confirmation(string Title, string Message, string Confirm, string Cancel, bool Danger, TaskCompletionSource<bool> Answer);

/// <summary>Toasts (the suite's <see cref="Toaster"/>) and confirmations: the friendly messages around every command.</summary>
public sealed class Ui : Toaster
{
    public Confirmation? Pending { get; private set; }

    public Task<bool> Confirm(string title, string message, string confirm, string cancel = "Cancel", bool danger = true)
    {
        Pending?.Answer.TrySetResult(false);
        Pending = new Confirmation(title, message, confirm, cancel, danger, new TaskCompletionSource<bool>());
        OnChanged();
        return Pending.Answer.Task;
    }

    public void Answer(bool yes)
    {
        var pending = Pending;
        Pending = null;
        pending?.Answer.TrySetResult(yes);
        OnChanged();
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
}
