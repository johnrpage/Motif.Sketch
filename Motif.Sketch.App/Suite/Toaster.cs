// Motif.NET shared web kit: copied from github.com/motif-dotnet/.github/tree/main/brand/web. Don't edit an app's copy.
namespace Motif.Suite;

public enum ToastKind { Info, Warning, Problem }

public sealed record Toast(int Id, ToastKind Kind, string Title, IReadOnlyList<string> Lines, string Copy = "");

/// <summary>
/// Toasts: the outcome of a command, shown at the bottom right (drawn by <see cref="ToastList"/>).
/// A title-only note goes after 3.5 s; anything with a sentence to read, and every warning or problem, stays 10 s.
/// A toast that offers text to copy is something to act on, so it stays until dismissed.
/// </summary>
public class Toaster
{
    private int next;

    public event Action? Changed;

    public List<Toast> Toasts { get; } = [];

    public void Say(string title, params string[] lines) => Show(ToastKind.Info, title, lines);
    public void Offer(string title, string line, string copy) => Show(ToastKind.Info, title, [line], copy);
    public void Warn(string title, IEnumerable<string> lines) => Show(ToastKind.Warning, title, [.. lines]);
    public void Problem(string title, IEnumerable<string> lines) => Show(ToastKind.Problem, title, [.. lines]);

    public void Dismiss(int id)
    {
        Toasts.RemoveAll(t => t.Id == id);
        OnChanged();
    }

    protected void OnChanged() => Changed?.Invoke();

    private void Show(ToastKind kind, string title, string[] lines, string copy = "")
    {
        var toast = new Toast(++next, kind, title, lines, copy);
        Toasts.Add(toast);
        if (Toasts.Count > 4) Toasts.RemoveAt(0);
        OnChanged();
        if (copy == "") _ = Expire(toast);
    }

    private async Task Expire(Toast toast)
    {
        await Task.Delay(toast.Kind == ToastKind.Info && toast.Lines.Count == 0 ? 3500 : 10000);
        Dismiss(toast.Id);
    }
}
