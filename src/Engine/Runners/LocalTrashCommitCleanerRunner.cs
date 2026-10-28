using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeployItSmart.Engine.ArgumentParser;
using DeployItSmart.Engine.Runner;
using DeployItSmart.Model.Configuration.Runner;
using DeployItSmart.Model.DataStructure;
using DeployItSmart.Model.DataStructure.Enums;
using LibGit2Sharp;

namespace DeployItSmart.Engine.Runners;

public class LocalTrashCommitCleanerRunner : BaseRunner
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
    public LocalTrashCommitCleanerRunner(SimpleArgumentParser simpleArgument)
    {
        CurrentReleaseVersion = new GetCurrentReleaseVersionRunner(simpleArgument);
        GetTrackCommitMessages = new GetTrackCommitMessagesRunner(simpleArgument);
        MessageTool = new MessageSearchTool();
    }

    public override async Task RunAsync()
    {
        await CleanCommits();
    }

    public async Task CleanCommits()
    {
        var currentVersion = await CurrentReleaseVersion.GetCurrentTag();
        string? sinceTag = currentVersion != default ? $"v{currentVersion.ToString(3)}" : null;
        var history = await GetTrackCommitMessages.GetHistory(sinceTag);
        if (history.Count <= 1) return;
        var lastCommit = history.First();
        if (lastCommit.CountDirtyToken() > 0 && lastCommit.CountReleaseToken() <= 0)
            return;

        Dictionary<TrackerReleaseType, List<string>> tokenMessages = new()
        {
            [TrackerReleaseType.Breaking] = new List<string>(),
            [TrackerReleaseType.Feature] = new List<string>(),
            [TrackerReleaseType.Fix] = new List<string>(),
            [TrackerReleaseType.Note] = new List<string>(),
            [TrackerReleaseType.Sponsor] = new List<string>(),
            [TrackerReleaseType.Thanks] = new List<string>(),
        };

        Dictionary<TrackerDirtyType, List<string>> tokenDirtyMessages = new()
        {
            [TrackerDirtyType.Trace] = new List<string>(),
            [TrackerDirtyType.TraceNoTrigger] = new List<string>(),
            [TrackerDirtyType.WorkInProgress] = new List<string>(),
        };

        TrackedCommitMessageStructure? resetPoint = default;
        var commits = history.ToList();
        foreach (var item in commits)
        {
            if (item != lastCommit)
                if (item.CountReleaseToken() <= 0)
                    resetPoint = item;
                else
                {
                    if (resetPoint != default)
                        resetPoint = item;
                    break;
                }
            foreach (var itemTokens in item.TokenMessages)
            {
                var toAdd = itemTokens.Value.Where(p => !tokenMessages[itemTokens.Key].Contains(p, StringComparer.OrdinalIgnoreCase))
                    .Select(p => AddReleaseTokenToMessage(itemTokens.Key, p));
                tokenMessages[itemTokens.Key].AddRange(toAdd);
            }
            foreach (var itemTokens in item.TokenDirtyMessages)
            {
                var toAdd = itemTokens.Value.Where(p => !tokenDirtyMessages[itemTokens.Key].Contains(p, StringComparer.OrdinalIgnoreCase))
                    .Select(p => AddDirtyTokenToMessage(itemTokens.Key, p));
                tokenDirtyMessages[itemTokens.Key].AddRange(toAdd);
            }

        }
        if (resetPoint == default || resetPoint == lastCommit)
        {
            System.Console.Error.WriteLine("No Reset Required Yet.");
            return;
        }

        var repo = new Repository(Path.Combine(Environment.CurrentDirectory));
        var commit = repo.Lookup<Commit>(resetPoint.CommitId); ;

        var nMessages = tokenMessages.SelectMany(p => p.Value).ToList();
        if (lastCommit.TokenDirtyMessages[TrackerDirtyType.WorkInProgress].Count > 0)
            foreach (var wip in lastCommit.TokenDirtyMessages[TrackerDirtyType.WorkInProgress])
                nMessages.Add(AddDirtyTokenToMessage(TrackerDirtyType.WorkInProgress, wip));

        var nCommitMessage = string.Join("\n", nMessages);
        if (string.IsNullOrWhiteSpace(nCommitMessage))
            throw new Exception("Couldn't extract messages.");
        repo.Reset(ResetMode.Mixed, commit);
        Commands.Stage(repo, "*");
        var cleanCommit = repo.Commit(nCommitMessage, commit.Author, commit.Author, new()
        {
            AllowEmptyCommit = true,
            PrettifyMessage = true
        });
        System.Console.WriteLine($"New Commit with work {cleanCommit.Message}");
        System.Console.WriteLine($"New Commit SHA-1 ID: {cleanCommit.Id.Sha}");
        System.Console.WriteLine($"Committed by: {cleanCommit.Committer.Name} at {cleanCommit.Committer.When}");
    }

    private string AddReleaseTokenToMessage(TrackerReleaseType tokenType, string message)
    {
        string token = string.Empty;
        if (tokenType == TrackerReleaseType.Breaking)
            token = MessageTool.ToolConfig.Configuration.ReleaseBreakingTokens[0];
        else if (tokenType == TrackerReleaseType.Feature)
            token = MessageTool.ToolConfig.Configuration.ReleaseFeatureTokens[0];
        else if (tokenType == TrackerReleaseType.Fix)
            token = MessageTool.ToolConfig.Configuration.ReleaseFixTokens[0];
        else if (tokenType == TrackerReleaseType.Note)
            token = MessageTool.ToolConfig.Configuration.ReleaseNoteTokens[0];
        else if (tokenType == TrackerReleaseType.Sponsor)
            token = MessageTool.ToolConfig.Configuration.ReleaseSponsorTokens[0];
        else
            token = MessageTool.ToolConfig.Configuration.ReleaseThanksTokens[0];
        return AddTokenKeyToMessage(token, message);
    }
    private string AddDirtyTokenToMessage(TrackerDirtyType tokenType, string message)
    {
        string token = string.Empty;
        if (tokenType == TrackerDirtyType.Trace)
            token = MessageTool.ToolConfig.Configuration.ReleaseNoLogTokens[0];
        else if (tokenType == TrackerDirtyType.TraceNoTrigger)
            token = MessageTool.ToolConfig.Configuration.ReleaseNoTriggerTokens[0];
        else
            token = MessageTool.ToolConfig.Configuration.ReleaseDirtyTokens[0];
        return AddTokenKeyToMessage(token, message);
    }
    private string AddTokenKeyToMessage(string tokenKey, string message)
    {
        string finalOut = string.Empty;
        string token = tokenKey;

        finalOut += WrapToken(token) + $" {message}";
        return finalOut;
    }

    private string WrapToken(string token)
    {
        string finalOut = string.Empty;
        if (!string.IsNullOrWhiteSpace(MessageTool.ToolConfig.Configuration.ReleaseLeftSymbol))
            finalOut += MessageTool.ToolConfig.Configuration.ReleaseLeftSymbol;
        finalOut += token;
        if (!string.IsNullOrWhiteSpace(MessageTool.ToolConfig.Configuration.ReleaseRightSymbol))
            finalOut += MessageTool.ToolConfig.Configuration.ReleaseRightSymbol;
        return finalOut;
    }
}
