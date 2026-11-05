using DeployItSmart.Engine.ArgumentParser;

namespace DeployItSmart.Model.Configuration;

public sealed record class CommitSearchParameters : BaseConfigurator
{
    public string? SinceTag { get; init; }
    public bool IsFirstRelease { get; init; }
    public bool AllowUnsafeVersionCalculation { get; init; }
    public CommitSearchParameters() : base()
    {
        SinceTag = StartupParams.GetOptionalOption("--sinceTag");
        AllowUnsafeVersionCalculation = StartupParams.CheckOptionalFlag("--allowUnsafeVersionFromSinceTag");
        IsFirstRelease = StartupParams.CheckOptionalFlag("--isFirstRelease");

    }
}
