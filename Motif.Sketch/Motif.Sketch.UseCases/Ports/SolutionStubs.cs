namespace Motif.Sketch;

/// <summary>Driven port: hands the solution stub plan on to be built. v1 writes it as a Markdown file.</summary>
public interface SolutionStubs
{
    /// <summary>Hands on the plan. False if the user cancelled.</summary>
    Task<bool> Write(string suggestedName, string markdown);
}
