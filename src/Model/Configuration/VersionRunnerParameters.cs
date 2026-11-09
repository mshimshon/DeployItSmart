using System;

namespace DeployItSmart.Model.Configuration;

public sealed record class VersionRunnerParameters : BaseConfigurator
{
    public bool GithubOutput { get; init; }
    public bool GithubOutputNullable { get; init; }
    public bool PatchStableOnly { get; init; }
    public bool IsFirstRelease { get; init; }
    public bool IsPreviewMode { get; init; }
    public VersionRunnerParameters() : base()
    {
        IsPreviewMode = StartupParams.CheckOptionalFlag("--previewMode");
        GithubOutput = StartupParams.CheckOptionalFlag("--ghOutput");
        PatchStableOnly = StartupParams.CheckOptionalFlag("--patchStabelOnly");
        IsFirstRelease = StartupParams.CheckOptionalFlag("--isFirstRelease");
    }
}
