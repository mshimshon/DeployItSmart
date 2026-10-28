using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Engine.Runner;
using DeployItSmart.Model.Configuration.Runner;
using DeployItSmart.Model.DataStructure;
using LibGit2Sharp;

namespace DeployItSmart.Engine.Runners;

public class CalculateNextReleaseVersionRunner : BaseRunner
{
    private JsonSerializerOptions jsonSerializerOption = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles

    };
    public GetCurrentReleaseVersionRunner CurrentReleaseVersion { get; init; }
    public GetTrackCommitMessagesRunner GetTrackCommitMessages { get; init; }
    public MessageSearchTool MessageTool { get; init; }
    public CalculateNextReleaseVersionRunner(SimpleArgumentParser simpleArgument)
    {
        CurrentReleaseVersion = new GetCurrentReleaseVersionRunner(simpleArgument);
        GetTrackCommitMessages = new GetTrackCommitMessagesRunner(simpleArgument);
        MessageTool = new MessageSearchTool();
    }

    public override async Task RunAsync()
    {
        var version = await GetNextVersion();
        Console.WriteLine(version);
    }

    public async Task<System.Version> GetNextVersion()
    {
        var currentVersion = await CurrentReleaseVersion.GetCurrentTag();
        if (currentVersion == default)
            return new System.Version(MessageTool.ToolConfig.Configuration.InitialVersion);

        var nextVersion = currentVersion;
        var commits = await GetTrackCommitMessages.GetHistory(currentVersion.ToString(3));
        var breaking = commits.SelectMany(p => p.TokenMessages[Model.DataStructure.Enums.TrackerReleaseType.Breaking]).Count();
        var feature = commits.SelectMany(p => p.TokenMessages[Model.DataStructure.Enums.TrackerReleaseType.Feature]).Count();
        if (breaking > 0)
            nextVersion = new(currentVersion.Major + 1, 0, 0);
        else if (feature > 0)
            nextVersion = new(currentVersion.Major, currentVersion.Minor + 1, 0);
        else
            nextVersion = new(currentVersion.Major, currentVersion.Minor, currentVersion.Build + 1);

        return nextVersion;
    }
}
