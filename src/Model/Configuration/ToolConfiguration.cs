namespace DeployItSmart.Model.Configuration;

public record class ToolConfiguration
{
    public string InitialVersion { get; init; } = "0.0.1";
    public string EmptyLogMessage { get; init; } = "This release has no information.";

    // Token ID Mappings (Represented as Tuples for key-value pair semantics)
    public string FeatureHeader { get; init; } = "New Features";
    public string BreakingHeader { get; init; } = "BREAKING CHANGES";
    public string FixHeader { get; init; } = "Fixes";
    public string NoteHeader { get; init; } = "Notes";
    public string SponsorHeader { get; init; } = "Release Sponsors";
    public string ThanksHeader { get; init; } = "Special Thanks";
    // Map Project to a Display Name, _ = Root or Main Changelogs
    public Dictionary<string, string> Projects { get; init; } = new()
    {
        {"_","My Project Name"}
    };
    // Core feature delivery tokens. Triggers standard minor semantic version increases.
    public IReadOnlyList<string> ReleaseFeatureTokens { get; init; } = new[] { "feat" };

    // Defect remediation tokens. Triggers standard patch semantic version increases.
    public IReadOnlyList<string> ReleaseFixTokens { get; init; } = new[] { "fix" };

    // Breaking change tokens. Forces a major version increment across delivery targets.
    public IReadOnlyList<string> ReleaseBreakingTokens { get; init; } = new[] { "feat!", "break" };

    // Supplementary release annotations. Captures critical operational notices.
    public IReadOnlyList<string> ReleaseNoteTokens { get; init; } = new[] { "note" };
    public IReadOnlyList<string> ReleaseSponsorTokens { get; init; } = new[] { "sponsor" };
    public IReadOnlyList<string> ReleaseThanksTokens { get; init; } = new[] { "thank" };

    // Work-In-Progress (WIP) definitions.
    public IReadOnlyList<string> ReleaseDirtyTokens { get; init; } = new[] { "wip" };

    // Workflow bypass tokens.
    public IReadOnlyList<string> ReleaseNoTriggerTokens { get; init; } = new[] { "trace!" };

    // Change-log omission filters.
    public IReadOnlyList<string> ReleaseNoLogTokens { get; init; } = new[] { "trace" };

    // Structural Commit Syntax Delimiters or Wrappers.
    public string ReleaseLeftSymbol { get; init; } = "";
    public string ReleaseRightSymbol { get; init; } = ":";

}
