using System;
using DeployItSmart.Engine.ArgumentParser;

namespace DeployItSmart.Model.Configuration;

public sealed record class ChangeLogParameters : BaseConfigurator
{
    public string OutputLogs { get; init; }
    public bool IncludeCommitLinks { get; init; }
    public bool FixNonLinearMaster { get; init; }
    public bool FailOnEmpty { get; init; }
    public ChangeLogParameters() : base()
    {
        string output = StartupParams.GetRequiredOption("--output");
        if (Path.IsPathFullyQualified(output))
            if (!Directory.Exists(output))
                throw new Exception($"--output directory must exist '{output}'.");
            else OutputLogs = output;
        else
            if (!Directory.Exists(Path.GetFullPath(output)))
                throw new Exception($"--output directory must exist '{output}'.");
            else
                OutputLogs = Path.GetFullPath(output);

        FailOnEmpty = StartupParams.CheckOptionalFlag("--autoFailEmptyResult");
        FixNonLinearMaster = StartupParams.CheckOptionalFlag("--nonLinearMasterFix");
        IncludeCommitLinks = StartupParams.CheckOptionalFlag("--linkCommits");
    }
}
