namespace Motif.Sketch.Fakes;

public sealed class FakeClipboard : Clipboard
{
    public string? Text { get; private set; }

    public Task Copy(string text)
    {
        Text = text;
        return Task.CompletedTask;
    }
}
