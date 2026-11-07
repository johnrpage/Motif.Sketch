namespace Motif.Sketch;

/// <summary>Thrown when an edit would break one of the project's rules. The project is left as it was.</summary>
public sealed class ProjectRuleException(string message) : Exception(message);
