using System;

namespace DeployItSmart.Model.Configuration;

public sealed record class VersionRunnerParameters : BaseConfigurator
{
    public bool GithubOutput { get; init; }
    public string? PreviewMark { get; init; } = "preview";
    public bool IsFirstRelease { get; init; }
    public bool IsPreviewMode { get; init; }
    public VersionRunnerParameters() : base()
    {
        IsPreviewMode = StartupParams.CheckOptionalFlag("--previewMode");
        GithubOutput = StartupParams.CheckOptionalFlag("--ghOutput");
        IsFirstRelease = StartupParams.CheckOptionalFlag("--isFirstRelease");
        PreviewMark = StartupParams.GetOptionalOption("--previewMark") ?? "preview";
    }
}
