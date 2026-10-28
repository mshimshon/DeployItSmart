using System;
using DeployItSmart.Engine.ArgumentParser;

namespace DeployItSmart.Model.Configuration.Runner;

public sealed record class GenerateChangeLogConf
{
    public string OutputLogs { get; init; }
    public bool IncludeCommitLinks { get; init; }
    public bool FailOnEmpty { get; init; }
    public GenerateChangeLogConf(SimpleArgumentParser simpleArgumentParser)
    {
        string output = simpleArgumentParser.GetRequiredOption("-output");
        if (Path.IsPathFullyQualified(output))
            if (!Directory.Exists(output))
                throw new Exception($"-output directory must exist '{output}'.");
            else OutputLogs = output;
        else
            if (!Directory.Exists(Path.GetFullPath(output)))
                throw new Exception($"-output directory must exist '{output}'.");
            else
                OutputLogs = Path.GetFullPath(output);

        FailOnEmpty = simpleArgumentParser.CheckOptionalFlag("-autoFailEmptyResult");
        IncludeCommitLinks = simpleArgumentParser.CheckOptionalFlag("-linkCommits");
    }
}
