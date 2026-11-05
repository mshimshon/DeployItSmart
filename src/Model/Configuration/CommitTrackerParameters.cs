using DeployItSmart.Engine.ArgumentParser;

namespace DeployItSmart.Model.Configuration;

public sealed record class CommitTrackerParameters : BaseConfigurator
{
    public bool KeepDuplicate { get; init; } = default;
    public CommitTrackerParameters() : base()
    {
        KeepDuplicate = StartupParams.CheckOptionalFlag("--keepDuplicate");
    }
}
