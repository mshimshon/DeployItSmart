using DeployItSmart.Engine.ArgumentParser;

namespace DeployItSmart.Model.Configuration.Runner;

public sealed record class GetCommitInfoSinceConf
{
    public string? SinceTag { get; init; }
    public bool SinceCreation => string.IsNullOrWhiteSpace(SinceTag);
    public GetCommitInfoSinceConf(SimpleArgumentParser simpleArgumentParser)
    {
        SinceTag = simpleArgumentParser.GetOptionalOption("-sinceTag");

    }
}
