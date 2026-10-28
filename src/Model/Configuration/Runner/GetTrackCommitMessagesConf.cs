using DeployItSmart.Engine.ArgumentParser;

namespace DeployItSmart.Model.Configuration.Runner;

public sealed record class GetTrackCommitMessagesConf
{
    public bool KeepDuplicate { get; init; } = default;
    public GetTrackCommitMessagesConf(SimpleArgumentParser simpleArgumentParser)
    {
        KeepDuplicate = simpleArgumentParser.CheckOptionalFlag("-keepDuplicate");
    }
}
