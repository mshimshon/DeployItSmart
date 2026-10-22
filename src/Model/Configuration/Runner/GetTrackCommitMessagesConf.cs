using DeployItSmart.Engine.ArgumentParser;

namespace DeployItSmart.Model.Configuration.Runner;

public sealed record class GetTrackCommitMessagesConf
{
    public string? OnlyProject { get; init; } = default;
    public bool KeepDuplicate { get; init; } = default;
    public GetTrackCommitMessagesConf(SimpleArgumentParser simpleArgumentParser)
    {
        OnlyProject = simpleArgumentParser.GetOptionalOption("-onlyProject");
        KeepDuplicate = simpleArgumentParser.CheckOptionalFlag("-keepDuplicate");
    }
}
