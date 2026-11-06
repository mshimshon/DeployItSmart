using System;

namespace DeployItSmart.Model.Configuration;

public sealed record class VersionRunnerParameters : BaseConfigurator
{
    public string? PreviewMark { get; init; } = "preview";
    public bool IsPreviewMode { get; init; }
    public VersionRunnerParameters() : base()
    {
        IsPreviewMode = StartupParams.CheckOptionalFlag("--previewMode");
        PreviewMark = StartupParams.GetOptionalOption("--previewMark") ?? "preview";
    }
}
