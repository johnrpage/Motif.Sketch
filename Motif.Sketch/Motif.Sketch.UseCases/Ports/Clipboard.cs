namespace Motif.Sketch;

/// <summary>Driven port: takes the snapshot link when the Architect copies it.</summary>
public interface Clipboard
{
    Task Copy(string text);
}
